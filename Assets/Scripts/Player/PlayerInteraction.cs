using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerInteraction : MonoBehaviour
{
    private readonly HashSet<InteractionZone> overlappingZones = new HashSet<InteractionZone>();

    private IInteractable currentInteractable;
    private InteractionZone currentZone;

    private void OnTriggerEnter(Collider other)
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

        if (currentZone == null)
        {
            ActivateZone(zone);
        }
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

        currentInteractable?.StopInteraction(this);
        currentInteractable = null;
        currentZone = null;

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

        currentInteractable?.StopInteraction(this);

        currentZone = zone;
        currentInteractable = interactable;
        currentInteractable.StartInteraction(this);
    }
}
