using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// In-headset debug menu. Created automatically in the Editor and in Development Builds, never in release builds,
/// so no scene setup is needed.
/// Toggle: left controller Menu button, or F1 on the keyboard. The panel floats above the left controller;
/// press its buttons with the right controller's ray.
/// </summary>
public class DebugMenu : MonoBehaviour
{
    private const float PanelWidth = 400f;
    private const float PanelHeight = 450f;
    private const float PanelScale = 0.0005f;
    private const float HeightAboveController = 0.15f;
    private const float InfoRefreshInterval = 0.25f;

    private InputAction toggleAction;
    private GameObject panel;
    private TextMeshProUGUI infoText;
    private Transform leftController;
    private Camera mainCamera;
    private float smoothedDeltaTime;
    private float nextInfoRefresh;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateInDebugBuilds()
    {
        if (!Debug.isDebugBuild) return;
        if (FindAnyObjectByType<DebugMenu>() != null) return;

        var menu = new GameObject("[Debug Menu]");
        DontDestroyOnLoad(menu);
        menu.AddComponent<DebugMenu>();
    }

    private void Awake()
    {
        if (!Debug.isDebugBuild)
        {
            Destroy(gameObject);
            return;
        }

        toggleAction = new InputAction("Toggle Debug Menu", InputActionType.Button);
        toggleAction.AddBinding("<XRController>{LeftHand}/{MenuButton}");
        toggleAction.AddBinding("<Keyboard>/f1");
        toggleAction.performed += _ => SetOpen(!panel.activeSelf);

        BuildPanel();
        SetOpen(false);
    }

    private void OnEnable() => toggleAction?.Enable();
    private void OnDisable() => toggleAction?.Disable();
    private void OnDestroy() => toggleAction?.Dispose();

    private void Update()
    {
        if (panel == null) return;

        smoothedDeltaTime = Mathf.Lerp(smoothedDeltaTime, Time.unscaledDeltaTime, 0.1f);

        if (!panel.activeSelf || Time.unscaledTime < nextInfoRefresh) return;
        nextInfoRefresh = Time.unscaledTime + InfoRefreshInterval;

        float fps = smoothedDeltaTime > 0f ? 1f / smoothedDeltaTime : 0f;
        infoText.text = $"{fps:0} FPS  ({smoothedDeltaTime * 1000f:0.0} ms)\n{DebugActions.GetPuzzleProgress()}\n{DebugActions.GetVaultCode()}";
    }

    private void LateUpdate()
    {
        if (panel != null && panel.activeSelf) FollowController();
    }

    private void SetOpen(bool open)
    {
        if (open)
        {
            EnsureEventSystem();
            FindRig();
            nextInfoRefresh = 0f;
            PlaceInFrontOfCamera();
        }

        panel.SetActive(open);
        if (open) FollowController();
    }

    private void FindRig()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        if (leftController == null)
        {
            var origin = FindAnyObjectByType<XROrigin>();
            if (origin != null)
            {
                foreach (Transform t in origin.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "Left Controller") { leftController = t; break; }
                }
            }
        }

        var canvas = panel.GetComponent<Canvas>();
        canvas.worldCamera = mainCamera;
    }

    private void FollowController()
    {
        if (leftController == null || mainCamera == null) return;

        Vector3 position = leftController.position + Vector3.up * HeightAboveController;
        panel.transform.position = position;
        panel.transform.rotation = Quaternion.LookRotation(position - mainCamera.transform.position, Vector3.up);
    }

    private void PlaceInFrontOfCamera()
    {
        if (mainCamera == null) return;

        Transform cam = mainCamera.transform;
        Vector3 forward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
        if (forward == Vector3.zero) forward = Vector3.forward;

        panel.transform.position = cam.position + forward * 0.6f;
        panel.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }

    private static void EnsureEventSystem()
    {
        var eventSystem = FindAnyObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            var go = new GameObject("EventSystem (Debug Menu)");
            go.AddComponent<EventSystem>();
            go.AddComponent<XRUIInputModule>();
            return;
        }

        if (eventSystem.GetComponent<XRUIInputModule>() == null)
        {
            Debug.LogWarning("Debug menu: the EventSystem has no XR UI Input Module, so controller rays can't press UI buttons.", eventSystem);
        }
    }

    private void BuildPanel()
    {
        panel = new GameObject("Debug Panel", typeof(RectTransform));
        panel.transform.SetParent(transform, false);
        panel.transform.localScale = Vector3.one * PanelScale;

        var canvas = panel.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 1000;
        panel.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 2f;
        panel.AddComponent<GraphicRaycaster>();
        panel.AddComponent<TrackedDeviceGraphicRaycaster>();

        var rect = (RectTransform)panel.transform;
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

        panel.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.92f);

        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 20, 20);
        layout.spacing = 12f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CreateText("Debug Menu", 34, FontStyles.Bold, 44f);
        infoText = CreateText("", 22, FontStyles.Normal, 100f);

        CreateButton("Skip Current Puzzle", DebugActions.SkipCurrentPuzzle);
        CreateButton("Reload Scene", () =>
        {
            SetOpen(false);
            DebugActions.ReloadScene();
        });
        CreateButton("Close", () => SetOpen(false));
    }

    private TextMeshProUGUI CreateText(string content, float size, FontStyles style, float height)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(panel.transform, false);
        go.AddComponent<LayoutElement>().preferredHeight = height;

        var text = go.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        return text;
    }

    private void CreateButton(string label, UnityAction onClick)
    {
        var go = new GameObject(label, typeof(RectTransform));
        go.transform.SetParent(panel.transform, false);
        go.AddComponent<LayoutElement>().preferredHeight = 64f;

        var image = go.AddComponent<Image>();
        image.color = new Color(0.22f, 0.24f, 0.3f, 1f);

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.highlightedColor = new Color(0.45f, 0.5f, 0.65f, 1f);
        colors.pressedColor = new Color(0.3f, 0.6f, 0.4f, 1f);
        button.colors = colors;
        button.onClick.AddListener(onClick);

        var textGo = new GameObject("Label", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var textRect = (RectTransform)textGo.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var text = textGo.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 26;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
    }
}