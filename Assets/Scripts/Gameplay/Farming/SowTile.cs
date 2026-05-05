using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SowTile : MonoBehaviour, IInteractable
{
    [Header("Config")]
    [SerializeField] private CropData cropData;
    [SerializeField] private Renderer tileRenderer;
    [SerializeField] private Transform cropSpawnPoint;
    [SerializeField] private Transform rewardSpawnPoint;

    [Header("Interaction Timings")]
    [SerializeField] private float sowTime = 1.25f;
    [SerializeField] private float harvestTime = 1f;

    [Header("Growth Modifiers")]
    [SerializeField] private float bonusGrowthMultiplier = 1.5f;
    [SerializeField] private bool startWithBonusApplied;
    [SerializeField] private bool crowPenaltyEnabled;
    [SerializeField] private float crowPenaltyMultiplier = 1.25f;

    [Header("Feedback")]
    [SerializeField] private Color emptyColor = new Color(0.45f, 0.28f, 0.12f, 1f);
    [SerializeField] private Color sowingColor = new Color(0.66f, 0.49f, 0.2f, 1f);
    [SerializeField] private Color readyColor = new Color(0.42f, 0.74f, 0.24f, 1f);
    [SerializeField] private Slider progressSlider;

    [Header("Audio")]
    [SerializeField] private AudioSource sowingSound;
    [SerializeField] private AudioSource harvestingSound;
    [SerializeField] private AudioSource harvestCompleteSound;

    [Header("Runtime")]
    [SerializeField] private SowState currentState = SowState.Empty;
    [SerializeField] private float stateTimer;
    [SerializeField] private float stateDuration;
    [SerializeField] private bool isBonusApplied;

    private Coroutine stateRoutine;
    private GameObject spawnedCropInstance;
    private bool isInteractionActive;

    public SowState CurrentState
    {
        get { return currentState; }
    }

    private void Awake()
    {
        if (tileRenderer == null)
        {
            tileRenderer = GetComponent<Renderer>();
        }

        isBonusApplied = startWithBonusApplied;
        ApplyStateVisuals();
        UpdateProgress(0f);
    }

    public void StartInteraction(PlayerInteraction player)
    {
        isInteractionActive = true;

        switch (currentState)
        {
            case SowState.Empty:
                BeginSowing();
                break;
            case SowState.ReadyToHarvest:
                BeginHarvesting();
                break;
        }
    }

    public void StopInteraction(PlayerInteraction player)
    {
        isInteractionActive = false;

        if (currentState == SowState.Sowing)
        {
            CancelInteractiveState(SowState.Empty);
            return;
        }

        if (currentState == SowState.Harvesting)
        {
            CancelInteractiveState(SowState.ReadyToHarvest);
        }
    }

    private void OnParticleCollision(GameObject other)
    {
        if (!isInteractionActive)
        {
            return;
        }

        if (currentState != SowState.Sowing && currentState != SowState.Empty)
        {
            return;
        }

        PlayerSeedEmitter seedEmitter = other.GetComponent<PlayerSeedEmitter>();
        if (seedEmitter == null)
        {
            seedEmitter = other.GetComponentInParent<PlayerSeedEmitter>();
        }

        if (seedEmitter == null)
        {
            return;
        }

        if (currentState == SowState.Empty)
        {
            BeginSowing();
        }

        float progressDelta;
        if (!seedEmitter.TryGetSowingProgress(gameObject, out progressDelta) || progressDelta <= 0f)
        {
            return;
        }

        AccumulateSowingProgress(progressDelta);
    }

    public SowTileSaveData CreateSaveData()
    {
        return new SowTileSaveData
        {
            position = transform.position,
            state = currentState,
            timer = stateTimer,
            cropId = cropData != null ? cropData.id : string.Empty,
            isBonusApplied = isBonusApplied
        };
    }

    public void ApplySaveData(SowTileSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        StopActiveRoutine();
        StopStateAudio();

        isBonusApplied = saveData.isBonusApplied;
        currentState = saveData.state;

        if (currentState == SowState.Sowing)
        {
            stateDuration = Mathf.Max(0.05f, sowTime);
            stateTimer = Mathf.Clamp(saveData.timer, 0f, stateDuration);
        }
        else
        {
            stateTimer = Mathf.Max(0f, saveData.timer);
            stateDuration = stateTimer;
        }

        if (currentState == SowState.Growing
            || currentState == SowState.ReadyToHarvest
            || currentState == SowState.Harvesting)
        {
            SpawnCropVisual();
        }
        else
        {
            ClearSpawnedCrop();
        }

        ApplyStateVisuals();

        if (currentState == SowState.Sowing && stateDuration > 0f)
        {
            UpdateProgress(stateTimer / stateDuration);
            return;
        }

        UpdateProgress(stateDuration > 0f ? 1f - (stateTimer / stateDuration) : 0f);
    }

    public void SetBonusApplied(bool value)
    {
        isBonusApplied = value;
    }

    private void BeginSowing()
    {
        if (!HasCropData())
        {
            return;
        }

        if (currentState == SowState.Sowing)
        {
            return;
        }

        StopActiveRoutine();
        StopIfAssigned(harvestingSound);

        currentState = SowState.Sowing;
        stateDuration = Mathf.Max(0.05f, sowTime);
        stateTimer = 0f;

        ApplyStateVisuals();
        UpdateProgress(0f);
        PlayIfAssigned(sowingSound);
    }

    private void AccumulateSowingProgress(float progressDelta)
    {
        if (currentState != SowState.Sowing)
        {
            return;
        }

        stateTimer = Mathf.Min(stateDuration, stateTimer + progressDelta);
        UpdateProgress(stateDuration > 0f ? stateTimer / stateDuration : 0f);

        if (stateTimer >= stateDuration)
        {
            CompleteSowing();
        }
    }

    private void CompleteSowing()
    {
        StopIfAssigned(sowingSound);
        stateTimer = 0f;
        stateDuration = 0f;

        SpawnCropVisual();
        StartGrowing(GetEffectiveGrowDuration());
    }

    private void StartGrowing(float duration)
    {
        StartTimedState(SowState.Growing, Mathf.Max(0.05f, duration), CompleteGrowing);
    }

    private void CompleteGrowing()
    {
        stateRoutine = null;
        stateTimer = 0f;
        stateDuration = 0f;
        currentState = SowState.ReadyToHarvest;
        ApplyStateVisuals();
        UpdateProgress(0f);
    }

    private void BeginHarvesting()
    {
        if (!HasCropData())
        {
            return;
        }

        EnsureCropVisual();
        PlayIfAssigned(harvestingSound);
        StartTimedState(SowState.Harvesting, Mathf.Max(0.05f, harvestTime), CompleteHarvesting);
    }

    private void CompleteHarvesting()
    {
        StopIfAssigned(harvestingSound);
        SpawnFinalItem();
        PlayIfAssigned(harvestCompleteSound);
        ClearSpawnedCrop();

        stateRoutine = null;
        stateTimer = 0f;
        stateDuration = 0f;
        currentState = SowState.Empty;
        ApplyStateVisuals();
        UpdateProgress(0f);
    }

    private void StartTimedState(SowState nextState, float duration, System.Action onCompleted)
    {
        StopActiveRoutine();
        stateRoutine = StartCoroutine(RunTimedState(nextState, duration, onCompleted));
    }

    private IEnumerator RunTimedState(SowState nextState, float duration, System.Action onCompleted)
    {
        currentState = nextState;
        stateDuration = duration;
        stateTimer = duration;

        ApplyStateVisuals();

        while (stateTimer > 0f)
        {
            stateTimer = Mathf.Max(0f, stateTimer - Time.deltaTime);
            UpdateProgress(1f - (stateTimer / stateDuration));
            yield return null;
        }

        onCompleted?.Invoke();
    }

    private void CancelInteractiveState(SowState fallbackState)
    {
        StopActiveRoutine();
        StopStateAudio();

        stateTimer = 0f;
        stateDuration = 0f;
        currentState = fallbackState;

        if (fallbackState == SowState.Empty)
        {
            ClearSpawnedCrop();
        }
        else if (fallbackState == SowState.ReadyToHarvest)
        {
            EnsureCropVisual();
        }

        ApplyStateVisuals();
        UpdateProgress(0f);
    }

    private void StopActiveRoutine()
    {
        if (stateRoutine == null)
        {
            return;
        }

        StopCoroutine(stateRoutine);
        stateRoutine = null;
    }

    private void StopStateAudio()
    {
        StopIfAssigned(sowingSound);
        StopIfAssigned(harvestingSound);
    }

    private bool HasCropData()
    {
        if (cropData != null)
        {
            return true;
        }

        Debug.LogWarning(string.Format("{0} cannot interact because CropData is missing.", name), this);
        return false;
    }

    private float GetEffectiveGrowDuration()
    {
        float duration = Mathf.Max(0.1f, cropData.growTime);

        if (isBonusApplied && bonusGrowthMultiplier > 0f)
        {
            duration /= bonusGrowthMultiplier;
        }

        if (crowPenaltyEnabled)
        {
            duration *= Mathf.Max(1f, crowPenaltyMultiplier);
        }

        return duration;
    }

    private void ApplyStateVisuals()
    {
        if (tileRenderer == null)
        {
            return;
        }

        Color targetColor = emptyColor;

        if (currentState == SowState.Sowing || currentState == SowState.Growing)
        {
            targetColor = sowingColor;
        }
        else if (currentState == SowState.ReadyToHarvest || currentState == SowState.Harvesting)
        {
            targetColor = readyColor;
        }

        tileRenderer.material.color = targetColor;
    }

    private void UpdateProgress(float normalizedProgress)
    {
        if (progressSlider == null)
        {
            return;
        }

        bool showProgress = currentState == SowState.Sowing
            || currentState == SowState.Growing
            || currentState == SowState.Harvesting;

        progressSlider.gameObject.SetActive(showProgress);
        progressSlider.normalizedValue = Mathf.Clamp01(normalizedProgress);
    }

    private void SpawnCropVisual()
    {
        if (cropData == null || cropData.cropPrefab == null || spawnedCropInstance != null)
        {
            return;
        }

        Transform anchor = cropSpawnPoint != null ? cropSpawnPoint : transform;
        Vector3 position = cropSpawnPoint != null
            ? cropSpawnPoint.position
            : GetRendererCenter() + Vector3.up * 0.35f;
        Quaternion rotation = cropSpawnPoint != null ? cropSpawnPoint.rotation : Quaternion.identity;

        spawnedCropInstance = Instantiate(cropData.cropPrefab, position, rotation, anchor);
    }

    private void EnsureCropVisual()
    {
        if (spawnedCropInstance == null)
        {
            SpawnCropVisual();
        }
    }

    private void ClearSpawnedCrop()
    {
        if (spawnedCropInstance == null)
        {
            return;
        }

        Destroy(spawnedCropInstance);
        spawnedCropInstance = null;
    }

    private void SpawnFinalItem()
    {
        if (cropData == null || cropData.finalItemPrefab == null)
        {
            return;
        }

        Transform anchor = rewardSpawnPoint != null ? rewardSpawnPoint : null;
        Vector3 position = rewardSpawnPoint != null
            ? rewardSpawnPoint.position
            : GetRendererCenter() + Vector3.up * 0.6f;
        Quaternion rotation = rewardSpawnPoint != null ? rewardSpawnPoint.rotation : Quaternion.identity;

        Instantiate(cropData.finalItemPrefab, position, rotation, anchor);
    }

    private Vector3 GetRendererCenter()
    {
        if (tileRenderer != null)
        {
            return tileRenderer.bounds.center;
        }

        return transform.position;
    }

    private void PlayIfAssigned(AudioSource audioSource)
    {
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
        }
    }

    private void StopIfAssigned(AudioSource audioSource)
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}
