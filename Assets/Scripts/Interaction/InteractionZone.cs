using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class InteractionZone : MonoBehaviour
{
    [SerializeField] private MonoBehaviour interactable;

    public IInteractable GetInteractable()
    {
        return interactable as IInteractable;
    }

    private void Reset()
    {
        Collider zoneCollider = GetComponent<Collider>();
        zoneCollider.isTrigger = true;

        if (interactable == null)
        {
            interactable = FindInteractableOnGameObject();
        }
    }

    private void OnValidate()
    {
        if (interactable == null)
        {
            interactable = FindInteractableOnGameObject();
            return;
        }

        if (!(interactable is IInteractable))
        {
            Debug.LogWarning($"{name} has an InteractionZone reference that does not implement IInteractable.", this);
            interactable = null;
        }
    }

    private MonoBehaviour FindInteractableOnGameObject()
    {
        MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();

        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour is IInteractable)
            {
                return behaviour;
            }
        }

        return null;
    }
}
