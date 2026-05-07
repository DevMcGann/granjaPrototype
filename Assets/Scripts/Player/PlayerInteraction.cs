using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerInteraction : MonoBehaviour
{
    private readonly HashSet<InteractionZone> overlappingZones = new HashSet<InteractionZone>();

    private IInteractable currentInteractable;
    private InteractionZone currentZone;
    private PlayerActionController actionController;

    private void Awake()
    {
        actionController = GetComponent<PlayerActionController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryRegisterZone(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryRegisterZone(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.isTrigger)
        {
            return;
        }

        InteractionZone zone = other.GetComponent<InteractionZone>();
        if (zone == null)
        {
            return;
        }

        overlappingZones.Remove(zone);

        if (zone != currentZone)
        {
            return;
        }

        StopCurrentInteraction();
        currentZone = null;
        ActivateNextOverlappingZone();
    }

    private void TryRegisterZone(Collider other)
    {
        if (!other.isTrigger)
        {
            return;
        }

        InteractionZone zone = other.GetComponent<InteractionZone>();
        if (zone == null)
        {
            return;
        }

        if (zone.GetInteractable() == null)
        {
            return;
        }

        overlappingZones.Add(zone);

        if (currentZone == null || !IsInteractableAlive(currentInteractable))
        {
            ActivateZone(zone);
        }
    }

    private void ActivateNextOverlappingZone()
    {
        foreach (InteractionZone overlappingZone in overlappingZones)
        {
            if (overlappingZone == null)
            {
                continue;
            }

            IInteractable interactable = overlappingZone.GetInteractable();
            if (interactable == null)
            {
                continue;
            }

            ActivateZone(overlappingZone);
            break;
        }
    }

    private void ActivateZone(InteractionZone zone)
    {
        if (zone == null)
        {
            return;
        }

        IInteractable interactable = zone.GetInteractable();
        if (interactable == null)
        {
            return;
        }

        if (currentZone == zone && currentInteractable == interactable)
        {
            return;
        }

        StopCurrentInteraction();

        currentZone = zone;
        currentInteractable = interactable;
        currentInteractable.StartInteraction(this);
    }

    private void StopCurrentInteraction()
    {
        if (!IsInteractableAlive(currentInteractable))
        {
            currentInteractable = null;
            return;
        }

        currentInteractable.StopInteraction(this);
        currentInteractable = null;
    }

    private static bool IsInteractableAlive(IInteractable interactable)
    {
        if (interactable == null)
        {
            return false;
        }

        Object unityObject = interactable as Object;
        return unityObject != null;
    }

    public PlayerActionController GetActionController()
    {
        if (actionController == null)
        {
            actionController = GetComponent<PlayerActionController>();
        }

        return actionController;
    }
}
