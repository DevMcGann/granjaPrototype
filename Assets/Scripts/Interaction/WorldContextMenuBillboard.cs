using UnityEngine;

[DisallowMultipleComponent]
public class WorldContextMenuBillboard : MonoBehaviour
{
    private const float CanvasWorldScale = 0.01f;
    private static readonly Quaternion CropAlignedRotation = Quaternion.Euler(90f, 0f, 0f);

    private Transform followTarget;
    private Vector3 localOffset;
    private Vector2 screenOffset;
    private Canvas canvas;
    private RectTransform rectTransform;

    public void SetTarget(Transform target, Vector3 offset, Vector2 uiScreenOffset)
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (canvas == null)
        {
            canvas = GetComponent<Canvas>();
        }

        followTarget = target;
        localOffset = offset;
        screenOffset = uiScreenOffset;
        UpdateTransform();
    }

    private void LateUpdate()
    {
        UpdateTransform();
    }

    private void UpdateTransform()
    {
        if (followTarget == null || rectTransform == null)
        {
            return;
        }

        if (rectTransform.parent != followTarget)
        {
            rectTransform.SetParent(followTarget, false);
        }

        if (canvas != null && canvas.worldCamera == null)
        {
            canvas.worldCamera = Camera.main;
        }

        Vector3 canvasPlaneOffset = new Vector3(
            screenOffset.x * CanvasWorldScale,
            0f,
            screenOffset.y * CanvasWorldScale);

        rectTransform.localPosition = localOffset + canvasPlaneOffset;
        rectTransform.localRotation = CropAlignedRotation;
        rectTransform.localScale = new Vector3(
            GetInverseScale(followTarget.lossyScale.x) * CanvasWorldScale,
            GetInverseScale(followTarget.lossyScale.z) * CanvasWorldScale,
            GetInverseScale(followTarget.lossyScale.y) * CanvasWorldScale);
    }

    private static float GetInverseScale(float value)
    {
        float safeScale = Mathf.Abs(value);
        if (safeScale < 0.0001f)
        {
            return 1f;
        }

        return 1f / safeScale;
    }
}
