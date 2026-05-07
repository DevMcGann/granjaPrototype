using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CropField : MonoBehaviour, IInteractable
{
    [Serializable]
    private class CropOption
    {
        public string menuLabel = "Corn";
        public CropSeedData cropSeed;
        public GameObject cropPrefab;
        public GameObject cropCratePrefab;
    }

    [Header("Default Crop")]
    [SerializeField] private CropSeedData cropSeed;
    [SerializeField] private GameObject cropPrefab;
    [SerializeField] private GameObject cropCratePrefab;

    [Header("Future Crop Options")]
    [SerializeField] private List<CropOption> cropOptions = new List<CropOption>();

    [Header("References")]
    [SerializeField] private Transform actionTrigger;
    [SerializeField] private Transform cropsRoot;

    [Header("World UI")]
    [SerializeField] private Vector3 menuOffset = new Vector3(-1.4f, 0f, 0f);
    [SerializeField] private Vector2 menuScreenOffset = Vector2.zero;
    [SerializeField] private Vector3 progressOffset = new Vector3(0f, 0f, -1f);
    [SerializeField] private Vector2 progressScreenOffset = Vector2.zero;

    [Header("Action Timings")]
    [SerializeField] private float defaultWaterTime = 5f;

    [Header("Crate Spawn")]
    [SerializeField] private float crateForwardOffset = 1.4f;
    [SerializeField] private float crateVerticalOffset = 0.35f;

    [Header("Crop Visual")]
    [SerializeField] private float hiddenCropsLocalY = -15f;
    [SerializeField] private float visibleCropsLocalY = 5f;

    [Header("Bonus")]
    [SerializeField] private bool isBonusApplied;
    [SerializeField] private float bonusMultiplier = 1.5f;

    [Header("Runtime")]
    [SerializeField] private FieldState currentState = FieldState.Empty;
    [SerializeField] private float stateTimer;
    [SerializeField] private string currentCropId;

    private Coroutine stateRoutine;
    private CropCrateInstance spawnedCrate;
    private PlayerInteraction currentPlayer;
    private PlayerActionController activePlayerActionController;
    private WorldContextMenu contextMenu;
    private Vector3 cropsDefaultLocalPosition;

    public FieldState CurrentState
    {
        get { return currentState; }
    }

    private void Awake()
    {
        CacheReferences();
        CacheDefaultVisualPosition();
        EnsureRuntimeMenu();
        ApplyStateVisualsImmediate();
    }

    private void OnEnable()
    {
        if (contextMenu != null)
        {
            contextMenu.Hide();
        }
    }

    private void OnDisable()
    {
        EndActivePlayerAction();
        HideMenu();
    }

    private void OnDestroy()
    {
        EndActivePlayerAction();

        if (contextMenu == null)
        {
            return;
        }

        Destroy(contextMenu.gameObject);
        contextMenu = null;
    }

    private void Reset()
    {
        CacheReferences();
        hiddenCropsLocalY = -15f;
        visibleCropsLocalY = 5f;
    }

    private void OnValidate()
    {
        CacheReferences();

        if (Mathf.Approximately(bonusMultiplier, 0f))
        {
            bonusMultiplier = 1f;
        }

        if (defaultWaterTime <= 0f)
        {
            defaultWaterTime = 5f;
        }

        if (!Application.isPlaying && cropsRoot != null)
        {
            Vector3 localPosition = cropsRoot.localPosition;
            localPosition.y = hiddenCropsLocalY;
            cropsRoot.localPosition = localPosition;
        }
    }

    public void StartInteraction(PlayerInteraction player)
    {
        if (player == null || !player.CompareTag("Player"))
        {
            return;
        }

        currentPlayer = player;
        ShowMenu();
    }

    public void StopInteraction(PlayerInteraction player)
    {
        if (player != null && player != currentPlayer)
        {
            return;
        }

        currentPlayer = null;

        if (ShouldShowProgress())
        {
            UpdateProgressVisual();
            return;
        }

        HideMenu();
    }

    public CropFieldSaveData GetSaveData()
    {
        return new CropFieldSaveData
        {
            position = transform.position,
            state = currentState,
            timer = Mathf.Max(0f, stateTimer),
            cropId = currentCropId ?? string.Empty,
            isBonusApplied = isBonusApplied
        };
    }

    public void LoadFromData(CropFieldSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        StopActiveRoutine();
        ClearCrate(notifyOwner: false);
        transform.position = saveData.position;
        isBonusApplied = saveData.isBonusApplied;
        currentCropId = saveData.cropId;
        currentState = saveData.state;
        stateTimer = Mathf.Max(0f, saveData.timer);

        switch (currentState)
        {
            case FieldState.Empty:
                currentCropId = string.Empty;
                ResetCropsVisual();
                break;
            case FieldState.Sowed:
                ResetCropsVisual();
                if (stateTimer > 0f)
                {
                    stateRoutine = StartCoroutine(RunSowedCountdown(stateTimer));
                }
                break;
            case FieldState.Watered:
                ResetCropsVisual();
                if (stateTimer > 0f)
                {
                    stateRoutine = StartCoroutine(RunWatering(stateTimer));
                }
                else
                {
                    StartGrowing(GetEffectiveGrowTime());
                }
                break;
            case FieldState.Growing:
                PrepareGrowingVisual();
                SetCropVisualPosition(GetGrowthProgressFromRemaining(stateTimer));
                stateRoutine = StartCoroutine(RunGrowing(stateTimer > 0f ? stateTimer : GetEffectiveGrowTime()));
                break;
            case FieldState.ReadyToHarvest:
                PrepareReadyToHarvestVisual();
                break;
            case FieldState.Harvesting:
                PrepareReadyToHarvestVisual();
                stateRoutine = StartCoroutine(RunHarvesting(stateTimer > 0f ? stateTimer : GetHarvestTime()));
                break;
            case FieldState.BlockedByCrate:
                ResetCropsVisual();
                SpawnCrateIfNeeded();
                break;
        }

        RefreshMenu();
    }

    public void SetBonusApplied(bool value)
    {
        isBonusApplied = value;
        RefreshMenu();
    }

    public void NotifyCrateRemoved(CropCrateInstance crate)
    {
        if (crate == null || crate != spawnedCrate)
        {
            return;
        }

        spawnedCrate = null;

        if (currentState != FieldState.BlockedByCrate)
        {
            return;
        }

        currentCropId = string.Empty;
        SetState(FieldState.Empty);
        stateTimer = 0f;
        ResetCropsVisual();
        RefreshMenu();
    }

    private void ShowMenu()
    {
        EnsureRuntimeMenu();
        RefreshMenu();

        if (ShouldShowProgress())
        {
            UpdateProgressVisual();
            return;
        }

        ApplyMenuFollowTarget();
        contextMenu.ShowMainMenu();
    }

    private void HideMenu()
    {
        if (contextMenu != null)
        {
            contextMenu.Hide();
        }
    }

    private void RefreshMenu()
    {
        if (contextMenu == null)
        {
            return;
        }

        if (ShouldShowProgress())
        {
            UpdateProgressVisual();
            return;
        }

        List<WorldContextMenu.MenuAction> mainActions = new List<WorldContextMenu.MenuAction>
        {
            new WorldContextMenu.MenuAction("Sow", CanOpenSowMenu(), OpenSowMenu),
            new WorldContextMenu.MenuAction("Water", CanWater(), TryWater),
            new WorldContextMenu.MenuAction("Harvest", CanHarvest(), TryHarvest)
        };

        contextMenu.ConfigureMainMenu("Crop Field", mainActions);
    }

    private void OpenSowMenu()
    {
        ApplyMenuFollowTarget();
        List<WorldContextMenu.MenuAction> sowActions = new List<WorldContextMenu.MenuAction>();

        foreach (CropOption option in GetAvailableOptions())
        {
            CropOption capturedOption = option;
            string label = GetMenuLabel(capturedOption);
            sowActions.Add(new WorldContextMenu.MenuAction(label, CanSow(capturedOption), () => TrySow(capturedOption)));
        }

        contextMenu.ConfigureSubMenu("Sow Crop", sowActions);
        contextMenu.ShowSubMenu();
    }

    private IEnumerable<CropOption> GetAvailableOptions()
    {
        bool hasExplicitOptions = false;
        for (int i = 0; i < cropOptions.Count; i++)
        {
            if (cropOptions[i] != null && cropOptions[i].cropSeed != null)
            {
                hasExplicitOptions = true;
                yield return cropOptions[i];
            }
        }

        if (hasExplicitOptions || cropSeed == null)
        {
            yield break;
        }

        yield return new CropOption
        {
            menuLabel = "Corn",
            cropSeed = cropSeed,
            cropPrefab = cropPrefab,
            cropCratePrefab = cropCratePrefab
        };
    }

    private string GetMenuLabel(CropOption option)
    {
        if (option == null)
        {
            return "Unknown";
        }

        if (!string.IsNullOrWhiteSpace(option.menuLabel))
        {
            return option.menuLabel;
        }

        if (option.cropSeed != null && !string.IsNullOrWhiteSpace(option.cropSeed.id))
        {
            return NicifyLabel(option.cropSeed.id);
        }

        return "Crop";
    }

    private bool CanOpenSowMenu()
    {
        if (currentState != FieldState.Empty || HasActiveTimer())
        {
            return false;
        }

        foreach (CropOption option in GetAvailableOptions())
        {
            if (CanSow(option))
            {
                return true;
            }
        }

        return false;
    }

    private bool CanSow(CropOption option)
    {
        return currentState == FieldState.Empty
            && !HasActiveTimer()
            && !IsCurrentPlayerBusy()
            && option != null
            && option.cropSeed != null
            && HasInventoryFor(option);
    }

    private bool CanWater()
    {
        return currentState == FieldState.Sowed && !HasActiveTimer() && !IsCurrentPlayerBusy();
    }

    private bool CanHarvest()
    {
        return currentState == FieldState.ReadyToHarvest && !HasActiveTimer() && !IsCurrentPlayerBusy();
    }

    private void TrySow(CropOption option)
    {
        if (!CanSow(option))
        {
            return;
        }

        if (!TryBeginPlayerAction(PlayerActionType.Sow))
        {
            return;
        }

        currentCropId = option.cropSeed.id;
        cropPrefab = option.cropPrefab != null ? option.cropPrefab : cropPrefab;
        cropCratePrefab = option.cropCratePrefab != null ? option.cropCratePrefab : cropCratePrefab;

        StopActiveRoutine();
        ResetCropsVisual();
        SetState(FieldState.Sowed);
        stateRoutine = StartCoroutine(RunSowedCountdown(GetSowTime()));
        UpdateProgressVisual();
        RefreshMenu();
    }

    private void TryWater()
    {
        if (!CanWater())
        {
            return;
        }

        if (!TryBeginPlayerAction(PlayerActionType.Water))
        {
            return;
        }

        StopActiveRoutine();
        SetState(FieldState.Watered);
        stateRoutine = StartCoroutine(RunWatering(GetWaterTime()));
        UpdateProgressVisual();
        RefreshMenu();
    }

    private void StartGrowing(float duration)
    {
        StopActiveRoutine();
        SetState(FieldState.Growing);
        PrepareGrowingVisual();
        stateRoutine = StartCoroutine(RunGrowing(Mathf.Max(0.05f, duration)));
        UpdateProgressVisual();
    }

    private void TryHarvest()
    {
        if (!CanHarvest())
        {
            return;
        }

        if (!TryBeginPlayerAction(PlayerActionType.Harvest))
        {
            return;
        }

        StopActiveRoutine();
        SetState(FieldState.Harvesting);
        stateRoutine = StartCoroutine(RunHarvesting(GetHarvestTime()));
        UpdateProgressVisual();
    }

    private IEnumerator RunSowedCountdown(float duration)
    {
        stateTimer = Mathf.Max(0.05f, duration);

        while (stateTimer > 0f)
        {
            stateTimer = Mathf.Max(0f, stateTimer - Time.deltaTime);
            UpdateProgressVisual();
            yield return null;
        }

        stateRoutine = null;
        EndActivePlayerAction();
        RestoreContextUiAfterAction();
    }

    private IEnumerator RunWatering(float duration)
    {
        stateTimer = Mathf.Max(0.05f, duration);

        while (stateTimer > 0f)
        {
            stateTimer = Mathf.Max(0f, stateTimer - Time.deltaTime);
            UpdateProgressVisual();
            yield return null;
        }

        stateRoutine = null;
        EndActivePlayerAction();
        StartGrowing(GetEffectiveGrowTime());
    }

    private IEnumerator RunGrowing(float duration)
    {
        float totalDuration = Mathf.Max(0.05f, duration);
        stateTimer = totalDuration;

        while (stateTimer > 0f)
        {
            stateTimer = Mathf.Max(0f, stateTimer - Time.deltaTime);
            SetCropVisualPosition(GetGrowthProgressFromRemaining(stateTimer));
            UpdateProgressVisual();
            yield return null;
        }

        stateRoutine = null;
        PrepareReadyToHarvestVisual();
        SetState(FieldState.ReadyToHarvest);
        RestoreContextUiAfterAction();
    }

    private IEnumerator RunHarvesting(float duration)
    {
        stateTimer = Mathf.Max(0.05f, duration);

        while (stateTimer > 0f)
        {
            stateTimer = Mathf.Max(0f, stateTimer - Time.deltaTime);
            UpdateProgressVisual();
            yield return null;
        }

        stateRoutine = null;
        stateTimer = 0f;
        EndActivePlayerAction();
        SpawnCrateIfNeeded();
        ResetCropsVisual();
        SetState(FieldState.BlockedByCrate);
        RestoreContextUiAfterAction();
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

    private void SetState(FieldState nextState)
    {
        currentState = nextState;
    }

    private bool HasActiveTimer()
    {
        return stateTimer > 0f;
    }

    private bool ShouldShowProgress()
    {
        return HasActiveTimer()
            && (currentState == FieldState.Sowed
            || currentState == FieldState.Watered
            || currentState == FieldState.Growing
            || currentState == FieldState.Harvesting);
    }

    private float GetSowTime()
    {
        CropSeedData selectedSeed = GetSelectedSeed();
        return selectedSeed != null ? Mathf.Max(0.05f, selectedSeed.sowTime) : 5f;
    }

    private float GetEffectiveGrowTime()
    {
        CropSeedData selectedSeed = GetSelectedSeed();
        float duration = selectedSeed != null ? Mathf.Max(0.05f, selectedSeed.growTime) : 5f;

        if (isBonusApplied && bonusMultiplier > 0f)
        {
            duration /= bonusMultiplier;
        }

        return Mathf.Max(0.05f, duration);
    }

    private float GetWaterTime()
    {
        return Mathf.Max(0.05f, defaultWaterTime);
    }

    private float GetHarvestTime()
    {
        CropSeedData selectedSeed = GetSelectedSeed();
        return selectedSeed != null ? Mathf.Max(0.05f, selectedSeed.harvestTime) : 5f;
    }

    private CropSeedData GetSelectedSeed()
    {
        CropOption option = ResolveCurrentOption();
        if (option != null && option.cropSeed != null)
        {
            return option.cropSeed;
        }

        return cropSeed;
    }

    private CropOption ResolveCurrentOption()
    {
        if (!string.IsNullOrWhiteSpace(currentCropId))
        {
            foreach (CropOption option in GetAvailableOptions())
            {
                if (option != null && option.cropSeed != null && option.cropSeed.id == currentCropId)
                {
                    return option;
                }
            }
        }

        return null;
    }

    private bool HasInventoryFor(CropOption option)
    {
        // Reserved for future inventory-based sow validation.
        return option != null && option.cropSeed != null;
    }

    private void PrepareGrowingVisual()
    {
        EnsureCropVisualFallback();

        if (cropsRoot == null)
        {
            return;
        }

        cropsRoot.gameObject.SetActive(true);
        SetCropVisualPosition(0f);
    }

    private void PrepareReadyToHarvestVisual()
    {
        EnsureCropVisualFallback();

        if (cropsRoot == null)
        {
            return;
        }

        cropsRoot.gameObject.SetActive(true);
        SetCropVisualPosition(1f);
    }

    private void ResetCropsVisual()
    {
        if (cropsRoot == null)
        {
            return;
        }

        cropsRoot.localPosition = new Vector3(cropsDefaultLocalPosition.x, hiddenCropsLocalY, cropsDefaultLocalPosition.z);
        cropsRoot.gameObject.SetActive(false);
    }

    private void ApplyStateVisualsImmediate()
    {
        switch (currentState)
        {
            case FieldState.Growing:
            case FieldState.ReadyToHarvest:
            case FieldState.Harvesting:
                PrepareReadyToHarvestVisual();
                break;
            default:
                ResetCropsVisual();
                break;
        }
    }

    private void CacheReferences()
    {
        if (actionTrigger == null)
        {
            Transform trigger = transform.Find("ActionTrigger");
            if (trigger != null)
            {
                actionTrigger = trigger;
            }
        }

        if (cropsRoot == null)
        {
            Transform crops = transform.Find("Crops");
            if (crops != null)
            {
                cropsRoot = crops;
            }
        }
    }

    private void CacheDefaultVisualPosition()
    {
        if (cropsRoot == null)
        {
            cropsDefaultLocalPosition = Vector3.zero;
            return;
        }

        cropsDefaultLocalPosition = cropsRoot.localPosition;
    }

    private void EnsureRuntimeMenu()
    {
        if (contextMenu != null)
        {
            ApplyMenuFollowTarget();
            return;
        }

        GameObject menuObject = new GameObject("WorldContextMenu", typeof(RectTransform));
        contextMenu = menuObject.AddComponent<WorldContextMenu>();
        ApplyMenuFollowTarget();
        contextMenu.Hide();
    }

    private void EnsureCropVisualFallback()
    {
        if (cropsRoot == null || cropsRoot.childCount > 0 || cropPrefab == null)
        {
            return;
        }

        Instantiate(cropPrefab, cropsRoot);
    }

    private float GetGrowthProgressFromRemaining(float remainingTime)
    {
        float totalDuration = GetEffectiveGrowTime();
        if (totalDuration <= 0f)
        {
            return 1f;
        }

        return Mathf.Clamp01(1f - (remainingTime / totalDuration));
    }

    private void SetCropVisualPosition(float normalizedProgress)
    {
        if (cropsRoot == null)
        {
            return;
        }

        float y = Mathf.Lerp(hiddenCropsLocalY, visibleCropsLocalY, Mathf.Clamp01(normalizedProgress));
        cropsRoot.localPosition = new Vector3(cropsDefaultLocalPosition.x, y, cropsDefaultLocalPosition.z);
    }

    private void SpawnCrateIfNeeded()
    {
        if (spawnedCrate != null)
        {
            return;
        }

        GameObject cratePrefabToSpawn = ResolveCratePrefab();
        if (cratePrefabToSpawn == null)
        {
            Debug.LogWarning($"{name} cannot spawn a crate because CropCratePrefab is missing.", this);
            return;
        }

        Vector3 spawnPosition = transform.position + (transform.forward * crateForwardOffset) + (Vector3.up * crateVerticalOffset);
        GameObject crateObject = Instantiate(cratePrefabToSpawn, spawnPosition, transform.rotation);
        spawnedCrate = crateObject.GetComponent<CropCrateInstance>();
        if (spawnedCrate == null)
        {
            spawnedCrate = crateObject.AddComponent<CropCrateInstance>();
        }

        spawnedCrate.Initialize(this);
    }

    private void ClearCrate(bool notifyOwner)
    {
        if (spawnedCrate == null)
        {
            return;
        }

        CropCrateInstance crate = spawnedCrate;
        spawnedCrate = null;
        crate.DetachOwner();

        if (notifyOwner)
        {
            Destroy(crate.gameObject);
            return;
        }

        if (crate != null)
        {
            Destroy(crate.gameObject);
        }
    }

    private GameObject ResolveCratePrefab()
    {
        CropOption option = ResolveCurrentOption();
        if (option != null && option.cropCratePrefab != null)
        {
            return option.cropCratePrefab;
        }

        return cropCratePrefab;
    }

    private static string NicifyLabel(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return "Crop";
        }

        string cleaned = rawValue.Replace("_", " ").Trim();
        if (cleaned.Length == 0)
        {
            return "Crop";
        }

        return char.ToUpperInvariant(cleaned[0]) + cleaned.Substring(1);
    }

    private void UpdateProgressVisual()
    {
        if (contextMenu == null || !ShouldShowProgress())
        {
            return;
        }

        ApplyProgressFollowTarget();
        contextMenu.ShowProgress("Crop Field", GetProgressLabel(), GetProgressNormalized());
    }

    private string GetProgressLabel()
    {
        switch (currentState)
        {
            case FieldState.Sowed:
                return "Sowing...";
            case FieldState.Watered:
                return "Watering...";
            case FieldState.Growing:
                return "Growing...";
            case FieldState.Harvesting:
                return "Harvesting...";
            default:
                return "Working...";
        }
    }

    private float GetProgressNormalized()
    {
        float duration = 1f;

        switch (currentState)
        {
            case FieldState.Sowed:
                duration = GetSowTime();
                break;
            case FieldState.Watered:
                duration = GetWaterTime();
                break;
            case FieldState.Growing:
                duration = GetEffectiveGrowTime();
                break;
            case FieldState.Harvesting:
                duration = GetHarvestTime();
                break;
        }

        if (duration <= 0f)
        {
            return 1f;
        }

        return Mathf.Clamp01(1f - (stateTimer / duration));
    }

    private void RestoreContextUiAfterAction()
    {
        RefreshMenu();

        if (currentPlayer != null)
        {
            ApplyMenuFollowTarget();
            contextMenu.ShowMainMenu();
            return;
        }

        HideMenu();
    }

    private void ApplyMenuFollowTarget()
    {
        if (contextMenu == null)
        {
            return;
        }

        contextMenu.SetFollowTarget(transform, menuOffset, menuScreenOffset);
    }

    private void ApplyProgressFollowTarget()
    {
        if (contextMenu == null)
        {
            return;
        }

        contextMenu.SetFollowTarget(transform, progressOffset, progressScreenOffset);
    }

    private bool TryBeginPlayerAction(PlayerActionType actionType)
    {
        PlayerActionController actionController = ResolveCurrentPlayerActionController();
        if (actionController == null)
        {
            Debug.LogWarning($"{name} cannot start {actionType} because the interacting player is missing PlayerActionController.", this);
            return false;
        }

        if (!actionController.BeginAction(actionType))
        {
            return false;
        }

        activePlayerActionController = actionController;
        return true;
    }

    private PlayerActionController ResolveCurrentPlayerActionController()
    {
        if (activePlayerActionController != null && activePlayerActionController.IsBusy)
        {
            return activePlayerActionController;
        }

        if (currentPlayer == null)
        {
            return null;
        }

        return currentPlayer.GetActionController();
    }

    private void EndActivePlayerAction()
    {
        if (activePlayerActionController == null)
        {
            return;
        }

        activePlayerActionController.EndAction();
        activePlayerActionController = null;
    }

    private bool IsCurrentPlayerBusy()
    {
        PlayerActionController actionController = ResolveCurrentPlayerActionController();
        return actionController != null && actionController.IsBusy;
    }
}
