using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 移动端 / WebGL 屏幕触控。
/// 直接读取 Input.touchCount / 鼠标位置判断按钮矩形，
/// 不依赖 EventSystem 的 UI 点击链路，保证手机浏览器上也能按住左右和射击。
/// </summary>
public class TouchControls : MonoBehaviour
{
    public static TouchControls Instance { get; private set; }

    public float Horizontal { get; private set; }
    public bool FireHeld { get; private set; }
    public bool IsVisible => root != null && root.activeSelf;

    private GameObject root;
    private RectTransform leftRect;
    private RectTransform rightRect;
    private RectTransform fireRect;
    private Image leftImage;
    private Image rightImage;
    private Image fireImage;
    private TMP_FontAsset fontAsset;

    private bool allowed;

    public void Initialize(RectTransform canvasRect, TMP_FontAsset font)
    {
        Instance = this;
        fontAsset = font;
        allowed = ShouldShow();

        if (root != null) return;

        EnsureEventSystem(canvasRect);
        BuildUI(canvasRect);
        SetVisible(allowed);
    }

    /// <summary>编辑器里不显示，方便键盘调试；WebGL、手机和触屏设备显示。</summary>
    public static bool ShouldShow()
    {
#if UNITY_EDITOR
        return false;
#else
        return Application.isMobilePlatform
            || Application.platform == RuntimePlatform.WebGLPlayer
            || Input.touchSupported;
#endif
    }

    public void SetVisible(bool visible)
    {
        if (root == null) return;

        if (!visible) ReleaseAll();
        root.SetActive(visible && allowed);
    }

    public void ReleaseAll()
    {
        Horizontal = 0f;
        FireHeld = false;
        SetVisual(leftImage, false);
        SetVisual(rightImage, false);
        SetVisual(fireImage, false);
    }

    private void Update()
    {
        if (root == null || !root.activeSelf)
        {
            Horizontal = 0f;
            FireHeld = false;
            return;
        }

        bool leftPressed = false;
        bool rightPressed = false;
        bool firePressed = false;

        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                Vector2 point = touch.position;
                leftPressed |= IsInside(leftRect, point);
                rightPressed |= IsInside(rightRect, point);
                firePressed |= IsInside(fireRect, point);
            }
        }
        else if (Input.GetMouseButton(0))
        {
            Vector2 point = Input.mousePosition;
            leftPressed = IsInside(leftRect, point);
            rightPressed = IsInside(rightRect, point);
            firePressed = IsInside(fireRect, point);
        }

        Horizontal = (rightPressed ? 1f : 0f) - (leftPressed ? 1f : 0f);
        FireHeld = firePressed;

        SetVisual(leftImage, leftPressed);
        SetVisual(rightImage, rightPressed);
        SetVisual(fireImage, firePressed);
    }

    private void BuildUI(RectTransform canvasRect)
    {
        root = new GameObject("TouchControls", typeof(RectTransform));
        root.transform.SetParent(canvasRect, false);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        root.transform.SetAsLastSibling();

        leftImage = CreateButton("TouchLeft", new Vector2(0f, 0f), new Vector2(115f, 145f), new Vector2(180f, 180f), "左", 48, out leftRect);
        rightImage = CreateButton("TouchRight", new Vector2(0f, 0f), new Vector2(305f, 145f), new Vector2(180f, 180f), "右", 48, out rightRect);
        fireImage = CreateButton("TouchFire", new Vector2(1f, 0f), new Vector2(-135f, 155f), new Vector2(210f, 210f), "射击", 34, out fireRect);
    }

    private Image CreateButton(string name, Vector2 anchor, Vector2 position, Vector2 size, string label, int fontSize, out RectTransform rect)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        buttonObject.transform.SetParent(root.transform, false);

        rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.42f);
        image.raycastTarget = false;

        GameObject labelObject = new GameObject(name + "Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = labelObject.GetComponent<TextMeshProUGUI>();
        text.font = fontAsset;
        text.text = label;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;

        return image;
    }

    private static bool IsInside(RectTransform rect, Vector2 screenPoint)
    {
        return rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, null);
    }

    private static void SetVisual(Image image, bool pressed)
    {
        if (image == null) return;
        Color current = image.color;
        image.color = new Color(current.r, current.g, current.b, pressed ? 0.68f : 0.42f);
    }

    private static void EnsureEventSystem(RectTransform canvasRect)
    {
        if (EventSystem.current == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        if (canvasRect.GetComponent<GraphicRaycaster>() == null)
        {
            canvasRect.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
