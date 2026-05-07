using UnityEngine;

[DisallowMultipleComponent]
public class CropCrateInstance : MonoBehaviour
{
    private CropField owner;
    private bool suppressOwnerNotification;

    public void Initialize(CropField fieldOwner)
    {
        owner = fieldOwner;
        suppressOwnerNotification = false;
    }

    public void DetachOwner()
    {
        suppressOwnerNotification = true;
        owner = null;
    }

    private void OnDestroy()
    {
        if (suppressOwnerNotification || owner == null)
        {
            return;
        }

        owner.NotifyCrateRemoved(this);
    }
}
