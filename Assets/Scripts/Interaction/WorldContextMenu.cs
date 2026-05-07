using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class WorldContextMenu : MonoBehaviour
{
    public readonly struct MenuAction
    {
        public MenuAction(string label, bool interactable, Action callback)
        {
            Label = label;
            Interactable = interactable;
            Callback = callback;
        }

        public string Label { get; }
        public bool Interactable { get; }
        public Action Callback { get; }
    }

    private readonly List<Button> buttonPool = new List<Button>();

    private const float PanelWidth = 320f;
    private const float ButtonHeight = 64f;
    private const float ButtonSpacing = 10f;
    private const float PanelSidePadding = 16f;
    private const float ButtonAreaTop = 92f;
    private const float ButtonAreaBottom = 16f;
    private const float ProgressAreaTop = 100f;
    private const float ProgressAreaBottom = 28f;
    private const float ProgressPanelHeight = 168f;
    private const float CanvasWorldScale = 0.01f;
    private const float CanvasHeight = 420f;

    private Canvas canvas;
    private RectTransform rootRect;
    private RectTransform panelRect;
    private RectTransform buttonAreaRect;
    private RectTransform progressAreaRect;
    private Text titleText;
    private Button backButton;
    private Text progressLabel;
    private Slider progressSlider;
    private WorldContextMenuBillboard billboard;
    private Font defaultFont;
    private List<MenuAction> mainActions = new List<MenuAction>();
    private List<MenuAction> subActions = new List<MenuAction>();
    private string mainTitle = string.Empty;
    private string subTitle = string.Empty;
    private bool showingSubMenu;

    private void Awake()
    {
        EnsureUi();
    }

    public void SetFollowTarget(Transform target, Vector3 worldOffset, Vector2 uiScreenOffset)
    {
        EnsureUi();
        billboard.SetTarget(target, worldOffset, uiScreenOffset);
    }

    public void ConfigureMainMenu(string title, List<MenuAction> actions)
    {
        mainTitle = title;
        mainActions = actions ?? new List<MenuAction>();

        if (!showingSubMenu)
        {
            RenderActions(mainTitle, mainActions, false);
        }
    }

    public void ConfigureSubMenu(string title, List<MenuAction> actions)
    {
        subTitle = title;
        subActions = actions ?? new List<MenuAction>();

        if (showingSubMenu)
        {
            RenderActions(subTitle, subActions, true);
        }
    }

    public void ShowMainMenu()
    {
        EnsureUi();
        showingSubMenu = false;
        RenderActions(mainTitle, mainActions, false);
        gameObject.SetActive(true);
    }

    public void ShowSubMenu()
    {
        EnsureUi();
        showingSubMenu = true;
        RenderActions(subTitle, subActions, true);
        gameObject.SetActive(true);
    }

    public void ShowProgress(string title, string actionLabel, float normalizedProgress)
    {
        EnsureUi();
        showingSubMenu = false;
        gameObject.SetActive(true);
        SetPanelHeight(ProgressPanelHeight);
        titleText.text = string.IsNullOrWhiteSpace(title) ? "Working" : title;
        backButton.gameObject.SetActive(false);
        buttonAreaRect.gameObject.SetActive(false);
        progressAreaRect.gameObject.SetActive(true);
        progressLabel.text = string.IsNullOrWhiteSpace(actionLabel) ? "Processing..." : actionLabel;
        progressSlider.normalizedValue = Mathf.Clamp01(normalizedProgress);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        showingSubMenu = false;
    }

    private void EnsureUi()
    {
        if (canvas != null)
        {
            return;
        }

        defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvas.sortingOrder = 50;

        gameObject.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;
        gameObject.AddComponent<GraphicRaycaster>();
        billboard = gameObject.AddComponent<WorldContextMenuBillboard>();

        rootRect = gameObject.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(PanelWidth, CanvasHeight);
        rootRect.localScale = Vector3.one * CanvasWorldScale;
        rootRect.pivot = new Vector2(0.5f, 0.5f);

        GameObject panelObject = CreateUiObject("Panel", transform);
        panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(PanelWidth, ProgressPanelHeight);

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0.12f, 0.15f, 0.18f, 0.92f);

        titleText = CreateText("Title", panelObject.transform, 24, FontStyle.Bold);
        titleText.alignment = TextAnchor.MiddleCenter;
        SetTopStretch(titleText.rectTransform, 12f, 12f, 30f);

        backButton = CreateButton("BackButton", panelObject.transform, "Back");
        backButton.onClick.AddListener(ShowMainMenu);
        SetTopStretch(backButton.GetComponent<RectTransform>(), 50f, 12f, 34f);

        GameObject buttonAreaObject = CreateUiObject("ButtonArea", panelObject.transform);
        buttonAreaRect = buttonAreaObject.GetComponent<RectTransform>();
        SetStretch(buttonAreaRect, PanelSidePadding, PanelSidePadding, ButtonAreaTop, ButtonAreaBottom);

        GameObject progressAreaObject = CreateUiObject("ProgressArea", panelObject.transform);
        progressAreaRect = progressAreaObject.GetComponent<RectTransform>();
        SetStretch(progressAreaRect, PanelSidePadding, PanelSidePadding, ProgressAreaTop, ProgressAreaBottom);

        progressLabel = CreateText("ProgressLabel", progressAreaObject.transform, 24, FontStyle.Normal);
        progressLabel.alignment = TextAnchor.MiddleCenter;
        SetTopStretch(progressLabel.rectTransform, 0f, 0f, 30f);

        GameObject sliderObject = CreateUiObject("ProgressBar", progressAreaObject.transform);
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        SetTopStretch(sliderRect, 48f, 0f, 28f);

        Image sliderBackground = sliderObject.AddComponent<Image>();
        sliderBackground.color = new Color(0.22f, 0.24f, 0.27f, 0.95f);

        progressSlider = sliderObject.AddComponent<Slider>();
        progressSlider.interactable = false;
        progressSlider.direction = Slider.Direction.LeftToRight;
        progressSlider.transition = Selectable.Transition.None;

        GameObject fillAreaObject = CreateUiObject("Fill Area", sliderObject.transform);
        RectTransform fillAreaRect = fillAreaObject.GetComponent<RectTransform>();
        SetStretch(fillAreaRect, 5f, 5f, 5f, 5f);

        GameObject fillObject = CreateUiObject("Fill", fillAreaObject.transform);
        Image fillImage = fillObject.AddComponent<Image>();
        fillImage.color = new Color(0.3f, 0.72f, 0.36f, 1f);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        StretchToParent(fillRect);

        progressSlider.fillRect = fillRect;
        progressSlider.targetGraphic = fillImage;
        progressSlider.value = 0f;
        progressAreaRect.gameObject.SetActive(false);
    }

    private void RenderActions(string title, List<MenuAction> actions, bool includeBackButton)
    {
        EnsureUi();
        SetPanelHeight(GetPanelHeightForActionCount(actions.Count));
        titleText.text = string.IsNullOrWhiteSpace(title) ? "Actions" : title;
        backButton.gameObject.SetActive(includeBackButton);
        buttonAreaRect.gameObject.SetActive(true);
        progressAreaRect.gameObject.SetActive(false);

        EnsureButtonPool(actions.Count);

        for (int i = 0; i < buttonPool.Count; i++)
        {
            bool active = i < actions.Count;
            Button button = buttonPool[i];
            button.gameObject.SetActive(active);

            if (!active)
            {
                continue;
            }

            PositionButton(button.GetComponent<RectTransform>(), i);

            MenuAction action = actions[i];
            Text label = button.GetComponentInChildren<Text>();
            label.text = action.Label;
            button.interactable = action.Interactable;
            button.onClick.RemoveAllListeners();

            if (action.Callback != null)
            {
                button.onClick.AddListener(() => action.Callback.Invoke());
            }
        }
    }

    private void EnsureButtonPool(int count)
    {
        while (buttonPool.Count < count)
        {
            Button button = CreateButton($"ActionButton_{buttonPool.Count}", buttonAreaRect.transform, "Action");
            buttonPool.Add(button);
        }
    }

    private void PositionButton(RectTransform buttonRect, int index)
    {
        float top = index * (ButtonHeight + ButtonSpacing);
        SetTopStretch(buttonRect, top, 0f, ButtonHeight);
    }

    private float GetPanelHeightForActionCount(int actionCount)
    {
        int safeCount = Mathf.Max(1, actionCount);
        float buttonsHeight = (safeCount * ButtonHeight) + ((safeCount - 1) * ButtonSpacing);
        return ButtonAreaTop + buttonsHeight + ButtonAreaBottom;
    }

    private void SetPanelHeight(float height)
    {
        panelRect.sizeDelta = new Vector2(PanelWidth, height);
    }

    private GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject uiObject = new GameObject(objectName, typeof(RectTransform));
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }

    private Text CreateText(string objectName, Transform parent, int fontSize, FontStyle fontStyle)
    {
        GameObject textObject = CreateUiObject(objectName, parent);
        Text text = textObject.AddComponent<Text>();
        text.font = defaultFont;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private Button CreateButton(string objectName, Transform parent, string labelText)
    {
        GameObject buttonObject = CreateUiObject(objectName, parent);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.2f, 0.45f, 0.29f, 0.96f);

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.2f, 0.45f, 0.29f, 0.96f);
        colors.highlightedColor = new Color(0.27f, 0.56f, 0.36f, 1f);
        colors.pressedColor = new Color(0.16f, 0.34f, 0.22f, 1f);
        colors.disabledColor = new Color(0.26f, 0.26f, 0.26f, 0.85f);
        button.colors = colors;

        Text label = CreateText("Label", buttonObject.transform, 26, FontStyle.Normal);
        label.text = labelText;
        label.alignment = TextAnchor.MiddleCenter;
        StretchToParent(label.rectTransform);

        return button;
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static void SetStretch(RectTransform rectTransform, float left, float right, float top, float bottom)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(left, bottom);
        rectTransform.offsetMax = new Vector2(-right, -top);
    }

    private static void SetTopStretch(RectTransform rectTransform, float top, float sideInset, float height)
    {
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.offsetMin = new Vector2(sideInset, -(top + height));
        rectTransform.offsetMax = new Vector2(-sideInset, -top);
    }
}
