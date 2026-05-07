using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class PrototypeFieldController : MonoBehaviour, IInteractable
{
    [Header("Lifecycle")]
    [SerializeField] private SowState initialState = SowState.Empty;
    [SerializeField] private GameObject sowedFieldPrefab;
    [SerializeField] private GameObject readyToHarvestFieldPrefab;
    [FormerlySerializedAs("cropCratePrefab")]
    [SerializeField] private GameObject outputPrefab;

    [Header("References")]
    [SerializeField] private Transform actionTrigger;

    [Header("Timings")]
    [SerializeField] private float sowDuration = 10f;
    [SerializeField] private float growDuration = 15f;
    [SerializeField] private float harvestDuration = 10f;
    [SerializeField] private float sowHitGracePeriod = 0.25f;

    [Header("Runtime")]
    [SerializeField] private SowState currentState = SowState.Empty;
    [SerializeField] private float stateTimer;
    [SerializeField] private float stateDuration;
    [SerializeField] private bool harvestCompleted;

    private Coroutine stateRoutine;
    private bool isInteractionActive;
    private bool hasInitialized;
    private float lastValidSowTime = float.NegativeInfinity;

    public SowState CurrentState
    {
        get { return currentState; }
    }

    private void Awake()
    {
        TryAssignActionTrigger();
    }

    private void Start()
    {
        InitializeState();
    }

    private void Reset()
    {
        TryAssignActionTrigger();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            TryAssignActionTrigger();
        }
    }

    public void StartInteraction(PlayerInteraction player)
    {
        if (harvestCompleted)
        {
            return;
        }

        isInteractionActive = true;

        if (currentState == SowState.ReadyToHarvest)
        {
            BeginHarvesting();
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

        if (currentState == SowState.Harvesting && !harvestCompleted)
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

        float progressDelta;
        if (!seedEmitter.TryGetSowingProgress(gameObject, out progressDelta) || progressDelta <= 0f)
        {
            return;
        }

        if (currentState == SowState.Empty)
        {
            BeginSowing();
        }

        RegisterValidSowingHit();
    }

    private void InitializeState()
    {
        if (hasInitialized)
        {
            return;
        }

        hasInitialized = true;
        currentState = initialState;
        stateTimer = 0f;
        stateDuration = 0f;
        harvestCompleted = false;

        if (initialState == SowState.Growing)
        {
            StartGrowing();
        }
    }

    private void BeginSowing()
    {
        if (currentState == SowState.Sowing)
        {
            return;
        }

        StopActiveRoutine();
        stateRoutine = StartCoroutine(RunSowingState());
    }

    private void CompleteSowing()
    {
        stateRoutine = null;
        stateTimer = 0f;
        stateDuration = 0f;
        ReplaceWithPrefab(sowedFieldPrefab);
    }

    private void StartGrowing()
    {
        StartTimedState(SowState.Growing, Mathf.Max(0.05f, growDuration), CompleteGrowing);
    }

    private void CompleteGrowing()
    {
        ReplaceWithPrefab(readyToHarvestFieldPrefab);
    }

    private void BeginHarvesting()
    {
        if (currentState == SowState.Harvesting)
        {
            return;
        }

        StartTimedState(SowState.Harvesting, Mathf.Max(0.05f, harvestDuration), CompleteHarvesting);
    }

    private void CompleteHarvesting()
    {
        stateRoutine = null;
        stateTimer = 0f;
        stateDuration = 0f;
        currentState = SowState.Harvesting;
        harvestCompleted = true;
        OnHarvestTimerCompleted();
        SpawnOutputPrefab();
    }

    private void OnHarvestTimerCompleted()
    {
        // Reserved for the later harvest animation hook.
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

        while (stateTimer > 0f)
        {
            stateTimer = Mathf.Max(0f, stateTimer - Time.deltaTime);
            yield return null;
        }

        onCompleted?.Invoke();
    }

    private IEnumerator RunSowingState()
    {
        currentState = SowState.Sowing;
        stateDuration = Mathf.Max(0.05f, sowDuration);
        stateTimer = 0f;

        while (stateTimer < stateDuration)
        {
            if (isInteractionActive && Time.time - lastValidSowTime <= Mathf.Max(0.01f, sowHitGracePeriod))
            {
                stateTimer = Mathf.Min(stateDuration, stateTimer + Time.deltaTime);
            }

            yield return null;
        }

        CompleteSowing();
    }

    private void CancelInteractiveState(SowState fallbackState)
    {
        StopActiveRoutine();
        stateTimer = 0f;
        stateDuration = 0f;
        currentState = fallbackState;
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

    private void RegisterValidSowingHit()
    {
        lastValidSowTime = Time.time;
    }

    private void ReplaceWithPrefab(GameObject replacementPrefab)
    {
        if (replacementPrefab == null)
        {
            Debug.LogWarning($"{name} cannot advance because the replacement prefab is missing.", this);
            return;
        }

        Transform currentTransform = transform;
        Transform parent = currentTransform.parent;
        int siblingIndex = currentTransform.GetSiblingIndex();

        GameObject replacement = Instantiate(replacementPrefab, parent);
        Transform replacementTransform = replacement.transform;
        replacementTransform.SetPositionAndRotation(currentTransform.position, currentTransform.rotation);
        replacementTransform.localScale = currentTransform.localScale;
        replacementTransform.SetSiblingIndex(siblingIndex);

        Destroy(gameObject);
    }

    private void SpawnOutputPrefab()
    {
        if (outputPrefab == null)
        {
            Debug.LogWarning($"{name} cannot spawn its output because the prefab reference is missing.", this);
            return;
        }

        Vector3 spawnPosition = actionTrigger != null ? actionTrigger.position : transform.position;
        Quaternion spawnRotation = actionTrigger != null ? actionTrigger.rotation : Quaternion.identity;
        Instantiate(outputPrefab, spawnPosition, spawnRotation);
    }

    private void TryAssignActionTrigger()
    {
        if (actionTrigger == null)
        {
            Transform trigger = transform.Find("ActionTrigger");
            if (trigger != null)
            {
                actionTrigger = trigger;
            }
        }
    }
}
