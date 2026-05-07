using UnityEngine;

[DisallowMultipleComponent]
public class PlayerActionController : MonoBehaviour
{
    private const string IdleStateName = "Idle";
    private const string SowStateName = "Sow";
    private const string WaterStateName = "Water";
    private const string HarvestStateName = "Harvest";
    private const string PickupStateName = "Pickup";

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int SowTriggerHash = Animator.StringToHash("Sow");
    private static readonly int WaterTriggerHash = Animator.StringToHash("Water");
    private static readonly int HarvestTriggerHash = Animator.StringToHash("Harvest");
    private static readonly int PickupTriggerHash = Animator.StringToHash("Pickup");

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Animation")]
    [SerializeField] private float actionStateCrossFadeDuration = 0.05f;
    [SerializeField] private float actionLoopRestartThreshold = 0.98f;

    private string activeStateName = string.Empty;
    private PlayerActionType activeAction;

    public bool IsBusy { get; private set; }

    private void Awake()
    {
        CacheReferences();
    }

    private void OnDisable()
    {
        EndAction();
    }

    private void Update()
    {
        if (!IsBusy || animator == null)
        {
            return;
        }

        animator.SetFloat(SpeedHash, 0f);

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (!stateInfo.IsName(activeStateName))
        {
            if (!animator.IsInTransition(0))
            {
                animator.CrossFadeInFixedTime(activeStateName, actionStateCrossFadeDuration, 0);
            }

            return;
        }

        if (!stateInfo.loop && stateInfo.normalizedTime >= actionLoopRestartThreshold)
        {
            animator.Play(activeStateName, 0, 0f);
        }
    }

    public bool BeginAction(PlayerActionType action)
    {
        CacheReferences();

        if (IsBusy)
        {
            return false;
        }

        if (animator == null)
        {
            Debug.LogWarning($"{name} cannot begin action {action} because no Animator was found.", this);
            return false;
        }

        activeAction = action;
        activeStateName = GetStateName(action);

        ResetActionTriggers();
        IsBusy = true;
        SetMovementLocked(true);
        animator.SetFloat(SpeedHash, 0f);
        animator.SetTrigger(GetTriggerHash(action));
        animator.CrossFadeInFixedTime(activeStateName, actionStateCrossFadeDuration, 0);
        return true;
    }

    public void EndAction()
    {
        if (!IsBusy)
        {
            return;
        }

        IsBusy = false;
        activeStateName = string.Empty;
        ResetActionTriggers();
        SetMovementLocked(false);

        if (animator != null)
        {
            animator.SetFloat(SpeedHash, 0f);
            animator.CrossFadeInFixedTime(IdleStateName, actionStateCrossFadeDuration, 0);
        }
    }

    private void CacheReferences()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }
    }

    private void SetMovementLocked(bool locked)
    {
        if (playerMovement == null)
        {
            Debug.LogWarning($"{name} is missing PlayerMovement. Action {activeAction} will animate but movement lock cannot be applied.", this);
            return;
        }

        playerMovement.SetMovementEnabled(!locked);
    }

    private void ResetActionTriggers()
    {
        if (animator == null)
        {
            return;
        }

        animator.ResetTrigger(SowTriggerHash);
        animator.ResetTrigger(WaterTriggerHash);
        animator.ResetTrigger(HarvestTriggerHash);
        animator.ResetTrigger(PickupTriggerHash);
    }

    private static int GetTriggerHash(PlayerActionType action)
    {
        switch (action)
        {
            case PlayerActionType.Sow:
                return SowTriggerHash;
            case PlayerActionType.Water:
                return WaterTriggerHash;
            case PlayerActionType.Harvest:
                return HarvestTriggerHash;
            case PlayerActionType.Pickup:
                return PickupTriggerHash;
            default:
                return SowTriggerHash;
        }
    }

    private static string GetStateName(PlayerActionType action)
    {
        switch (action)
        {
            case PlayerActionType.Sow:
                return SowStateName;
            case PlayerActionType.Water:
                return WaterStateName;
            case PlayerActionType.Harvest:
                return HarvestStateName;
            case PlayerActionType.Pickup:
                return PickupStateName;
            default:
                return IdleStateName;
        }
    }
}
