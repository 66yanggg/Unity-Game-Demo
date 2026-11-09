using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 游戏 UI 与状态控制：玩家血量、分数、关卡 HUD、过关/失败结算。
/// 所有运行时文字使用 TextMeshPro，并统一使用项目内置的中文字体图集，
/// 避免 WebGL 下因为缺少系统中文字体而出现空白。
/// </summary>
public class GameController : MonoBehaviour
{
    private Transform hearts;
    private TextMeshProUGUI scoreText;
    private Transform endPanel;
    private Image resultImage;
    private TextMeshProUGUI resultText;
    private TextMeshProUGUI resultSubText;
    private Button primaryButton;
    private Button exitButton;
    private TextMeshProUGUI primaryLabel;
    private TextMeshProUGUI exitLabel;

    private GameObject hudRoot;
    private TextMeshProUGUI levelText;
    private TextMeshProUGUI targetText;
    private TextMeshProUGUI hintText;
    private TMP_FontAsset uiFont;

    private LevelManager levelManager;
    private Playercontroller playerController;
    private TouchControls touchControls;

    private int maxHp;
    private int hpValue;
    private int scoreValue;
    private bool isGameOver;
    private float invulnerableUntil;
    private ResultAction resultAction;

    public bool IsGameOver => isGameOver;
    public int Score => scoreValue;

    private enum ResultAction
    {
        None,
        NextLevel,
        Retry,
        RestartGame
    }

    private void Start()
    {
        uiFont = LoadTmpFont();

        if (!CacheSceneUi())
        {
            enabled = false;
            return;
        }

        BuildHud();
        CreateTouchControls();
        StartCoroutine(ApplyOutlinesWhenReady());

        levelManager = GetComponent<LevelManager>();
        if (levelManager == null)
        {
            levelManager = gameObject.AddComponent<LevelManager>();
        }
        levelManager.Initialize(this);

        if (GameFacade.Instance != null)
        {
            GameFacade.Instance.RegisterLevelManager(levelManager);
        }

        StartCoroutine(BeginGameRoutine());
    }

    private TMP_FontAsset LoadTmpFont()
    {
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts/GameChineseFont");
        if (font == null)
        {
            Debug.LogError("[TMP] 找不到 Assets/Resources/Fonts/GameChineseFont.asset，" +
                           "请先执行 Tools/TextMeshPro/重建游戏中文 TMP 字体。");
            font = TMP_Settings.defaultFontAsset;
        }
        return font;
    }

    private bool CacheSceneUi()
    {
        // 场景原本使用的是旧版 Text，这里在运行时全部替换为 TMP。
        ConvertLegacyTexts();

        hearts = transform.Find("HpImg");
        Transform scoreRoot = transform.Find("ScoreText/score");
        scoreText = scoreRoot != null ? scoreRoot.GetComponent<TextMeshProUGUI>() : null;
        endPanel = transform.Find("endPanel");

        if (hearts == null || scoreText == null || endPanel == null)
        {
            Debug.LogError("[UI] 场景 UI 结构不完整，需要 HpImg / ScoreText/score / endPanel。");
            return false;
        }

        maxHp = Mathf.Max(1, hearts.childCount);
        hpValue = maxHp;

        resultImage = endPanel.GetChild(0).GetComponent<Image>();
        primaryButton = endPanel.GetChild(1).GetComponent<Button>();
        exitButton = endPanel.GetChild(2).GetComponent<Button>();

        if (resultImage == null || primaryButton == null || exitButton == null)
        {
            Debug.LogError("[UI] endPanel 结构不完整，需要 Image / 重开按钮 / 退出按钮。");
            return false;
        }

        primaryLabel = primaryButton.GetComponentInChildren<TextMeshProUGUI>(true);
        exitLabel = exitButton.GetComponentInChildren<TextMeshProUGUI>(true);

        primaryButton.onClick.RemoveAllListeners();
        primaryButton.onClick.AddListener(OnPrimaryClicked);
        exitButton.onClick.RemoveAllListeners();
        exitButton.onClick.AddListener(OnExitClicked);

        // 移动端 / 网页上把结算按钮做得更大，方便手指点击。
        ResizeButton(primaryButton, new Vector2(260f, 80f), new Vector2(-170f, -150f));
        ResizeButton(exitButton, new Vector2(260f, 80f), new Vector2(170f, -150f));

        if (primaryLabel != null) primaryLabel.fontSize = 30f;
        if (exitLabel != null) exitLabel.fontSize = 30f;

#if UNITY_WEBGL && !UNITY_EDITOR
        if (exitLabel != null) exitLabel.text = "打开主页";
#else
        if (exitLabel != null) exitLabel.text = "退出游戏";
#endif

        playerController = FindObjectOfType<Playercontroller>();

        // 原分数文字是深灰色，这里加白字和描边，保证在草地背景上清晰。
        TextMeshProUGUI scoreLabel = scoreText.transform.parent.GetComponent<TextMeshProUGUI>();
        if (scoreLabel != null)
        {
            scoreLabel.color = Color.white;
        }
        scoreText.color = Color.white;

        resultImage.preserveAspect = true;
        return true;
    }

    private void ConvertLegacyTexts()
    {
        Text[] legacyTexts = GetComponentsInChildren<Text>(true);
        for (int i = 0; i < legacyTexts.Length; i++)
        {
            ConvertLegacyText(legacyTexts[i]);
        }
    }

    private void ConvertLegacyText(Text legacy)
    {
        if (legacy == null) return;
        if (legacy.GetComponent<TextMeshProUGUI>() != null) return;

        GameObject target = legacy.gameObject;
        string content = legacy.text;
        int fontSize = legacy.fontSize;
        Color color = legacy.color;
        TextAnchor anchor = legacy.alignment;
        bool raycastTarget = legacy.raycastTarget;
        FontStyle fontStyle = legacy.fontStyle;
        float lineSpacing = legacy.lineSpacing;
        bool wrap = legacy.horizontalOverflow == HorizontalWrapMode.Wrap;
        bool overflow = legacy.verticalOverflow == VerticalWrapMode.Overflow;

        // 一个 GameObject 只能有一个 Graphic。
        // 必须先移除旧 Text，再添加 TMP，否则运行时会报
        // "A GameObject can only contain one 'Graphic' component"。
        DestroyImmediate(legacy);

        // TMP 在 TMP Settings 尚未加载时会提前 return，自己不一定能补上
        // CanvasRenderer；这里先确保组件存在。
        if (target.GetComponent<CanvasRenderer>() == null)
        {
            target.AddComponent<CanvasRenderer>();
        }

        TextMeshProUGUI tmp = target.AddComponent<TextMeshProUGUI>();
        tmp.font = uiFont;
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = ToTmpAlignment(anchor);
        tmp.raycastTarget = raycastTarget;
        tmp.enableWordWrapping = wrap;
        tmp.overflowMode = overflow ? TextOverflowModes.Overflow : TextOverflowModes.Truncate;
        tmp.fontStyle = ToTmpStyle(fontStyle);
        tmp.lineSpacing = lineSpacing;
    }

    private void BuildHud()
    {
        GameObject hudObject = new GameObject("LevelHud", typeof(RectTransform));
        hudObject.transform.SetParent(transform, false);
        RectTransform hudRect = hudObject.GetComponent<RectTransform>();
        hudRect.anchorMin = Vector2.zero;
        hudRect.anchorMax = Vector2.one;
        hudRect.offsetMin = Vector2.zero;
        hudRect.offsetMax = Vector2.zero;
        hudRoot = hudObject;

        GameObject barObject = new GameObject("TopBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        barObject.transform.SetParent(hudObject.transform, false);
        RectTransform barRect = barObject.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.5f, 1f);
        barRect.anchorMax = new Vector2(0.5f, 1f);
        barRect.pivot = new Vector2(0.5f, 1f);
        barRect.anchoredPosition = new Vector2(0f, -10f);
        barRect.sizeDelta = new Vector2(660f, 108f);
        Image barImage = barObject.GetComponent<Image>();
        barImage.color = new Color(0f, 0f, 0f, 0.42f);
        barImage.raycastTarget = false;

        levelText = CreateText(
            "LevelText", hudObject.transform,
            new Vector2(0.5f, 1f), new Vector2(0f, -26f),
            new Vector2(1000f, 46f), 32, TextAlignmentOptions.Center);

        targetText = CreateText(
            "TargetText", hudObject.transform,
            new Vector2(0.5f, 1f), new Vector2(0f, -72f),
            new Vector2(800f, 34f), 22, TextAlignmentOptions.Center);

        hintText = CreateText(
            "HintText", hudObject.transform,
            new Vector2(0.5f, 0f), new Vector2(0f, 26f),
            new Vector2(1000f, 30f), 20, TextAlignmentOptions.Center);
        hintText.color = new Color(1f, 1f, 1f, 0.8f);
        hintText.text = "A / D 或 ← → 移动   ·   按住空格连续射击";

        resultText = CreateText(
            "ResultTitle", endPanel,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 168f),
            new Vector2(1100f, 84f), 46, TextAlignmentOptions.Center);

        resultSubText = CreateText(
            "ResultSub", endPanel,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 104f),
            new Vector2(1000f, 42f), 26, TextAlignmentOptions.Center);
        resultSubText.color = new Color(1f, 0.95f, 0.75f);

        if (primaryLabel != null) primaryLabel.font = uiFont;
        if (exitLabel != null) exitLabel.font = uiFont;
    }

    private void CreateTouchControls()
    {
        touchControls = GetComponent<TouchControls>();
        if (touchControls == null)
        {
            touchControls = gameObject.AddComponent<TouchControls>();
        }

        touchControls.Initialize(transform as RectTransform, uiFont);
    }

    private TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        Vector2 anchor,
        Vector2 anchoredPosition,
        Vector2 size,
        int fontSize,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = uiFont;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;

        return text;
    }

    private IEnumerator ApplyOutlinesWhenReady()
    {
        // TMP Settings / 材质在第一帧初始化，过早设置 outline 会访问空材质。
        for (int i = 0; i < 10 && TMP_Settings.instance == null; i++)
        {
            yield return null;
        }

        yield return null;

        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TextMeshProUGUI text = texts[i];
            if (text == null) continue;

            try
            {
                text.ForceMeshUpdate();
                ApplyTmpOutline(text);
            }
            catch (Exception)
            {
                // 单个文字对象初始化失败不应影响整个关卡启动。
            }
        }
    }

    private static void ApplyTmpOutline(TMP_Text text)
    {
        if (text == null) return;
        text.outlineWidth = 0.12f;
        text.outlineColor = new Color32(0, 0, 0, 220);
    }

    private static TextAlignmentOptions ToTmpAlignment(TextAnchor anchor)
    {
        switch (anchor)
        {
            case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
            case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
            case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
            case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
            case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
            case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
            case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
            case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
            case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
            default: return TextAlignmentOptions.Center;
        }
    }

    private static FontStyles ToTmpStyle(FontStyle style)
    {
        switch (style)
        {
            case FontStyle.Bold: return FontStyles.Bold;
            case FontStyle.Italic: return FontStyles.Italic;
            case FontStyle.BoldAndItalic: return FontStyles.Bold | FontStyles.Italic;
            default: return FontStyles.Normal;
        }
    }

    private static void ResizeButton(Button button, Vector2 size, Vector2 position)
    {
        if (button == null) return;
        RectTransform rect = button.transform as RectTransform;
        if (rect == null) return;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private IEnumerator BeginGameRoutine()
    {
        yield return null;
        if (levelManager != null)
        {
            levelManager.StartGame();
        }
    }

    public void PrepareLevel(int levelIndex, int totalLevels, int currentKills, int target, string levelName)
    {
        isGameOver = false;
        resultAction = ResultAction.None;
        hpValue = maxHp;
        invulnerableUntil = 0f;

        RefreshHearts();
        if (hearts != null) hearts.gameObject.SetActive(true);
        if (endPanel != null) endPanel.gameObject.SetActive(false);
        if (hudRoot != null) hudRoot.SetActive(true);

        if (touchControls != null)
        {
            touchControls.ReleaseAll();
            touchControls.SetVisible(true);
        }

        if (playerController != null)
        {
            playerController.ResetForLevel();
        }

        if (scoreText != null) scoreText.text = scoreValue.ToString();
        if (levelText != null)
        {
            levelText.text = "第 " + (levelIndex + 1) + " / " + totalLevels + " 关   ·   " + levelName;
        }
        if (targetText != null)
        {
            targetText.text = "击杀 " + currentKills + " / " + target;
        }
    }

    public void UpdateKillProgress(int kills, int target)
    {
        if (targetText != null)
        {
            targetText.text = "击杀 " + kills + " / " + target;
        }
    }

    public void AddScore(int amount)
    {
        scoreValue += Mathf.Max(0, amount);
        if (scoreText != null) scoreText.text = scoreValue.ToString();
    }

    public void SetScore(int value)
    {
        scoreValue = Mathf.Max(0, value);
        if (scoreText != null) scoreText.text = scoreValue.ToString();
    }

    public bool PlayerTakeDamage(int damage)
    {
        if (isGameOver || damage <= 0 || Time.time < invulnerableUntil)
        {
            return false;
        }

        invulnerableUntil = Time.time + 1.1f;
        hpValue = Mathf.Max(0, hpValue - damage);
        RefreshHearts();

        if (GameFacade.Instance != null)
        {
            GameFacade.Instance.PlaySound("hachimi aminuos", 0.9f);
        }

        if (playerController != null)
        {
            playerController.PlayHitFeedback();
        }

        if (hpValue <= 0)
        {
            isGameOver = true;
            if (levelManager != null)
            {
                levelManager.OnPlayerDefeated();
            }
            else
            {
                ShowGameOver(0, 0);
            }
        }

        return true;
    }

    public void ShowLevelClear(int levelIndex, int totalLevels, bool isFinal)
    {
        isGameOver = true;
        resultAction = isFinal ? ResultAction.RestartGame : ResultAction.NextLevel;

        HideHudForResult();

        if (resultImage != null && GameFacade.Instance != null)
        {
            resultImage.sprite = GameFacade.Instance.LoadImg("胜利(victory)");
        }

        if (resultText != null)
        {
            resultText.text = isFinal
                ? "全部关卡通关！"
                : "第 " + (levelIndex + 1) + " 关完成！";
        }

        if (resultSubText != null)
        {
            resultSubText.text = isFinal
                ? "最终得分：" + scoreValue
                : "得分：" + scoreValue + "   ·   准备下一关";
        }

        SetPrimaryLabel(isFinal ? "再玩一次" : "下一关");

        if (endPanel != null) endPanel.gameObject.SetActive(true);

        if (GameFacade.Instance != null)
        {
            GameFacade.Instance.PlaySound("hachimi live family laughter", 0.9f);
        }
    }

    public void ShowGameOver(int levelIndex, int totalLevels)
    {
        isGameOver = true;
        resultAction = ResultAction.Retry;

        HideHudForResult();

        if (resultImage != null && GameFacade.Instance != null)
        {
            resultImage.sprite = GameFacade.Instance.LoadImg("失败(defeat)");
        }

        if (resultText != null)
        {
            resultText.text = "游戏结束";
        }

        if (resultSubText != null)
        {
            int total = Mathf.Max(1, totalLevels);
            resultSubText.text = "第 " + (levelIndex + 1) + " / " + total + " 关失败   ·   得分：" + scoreValue;
        }

        SetPrimaryLabel("重试本关");

        if (endPanel != null) endPanel.gameObject.SetActive(true);

        if (GameFacade.Instance != null)
        {
            GameFacade.Instance.PlaySound("haha", 0.9f);
        }
    }

    public void SetGameOver(bool value)
    {
        isGameOver = value;
    }

    private void HideHudForResult()
    {
        if (hudRoot != null) hudRoot.SetActive(false);
        if (hearts != null) hearts.gameObject.SetActive(false);
        if (touchControls != null)
        {
            touchControls.ReleaseAll();
            touchControls.SetVisible(false);
        }
    }

    private void SetPrimaryLabel(string label)
    {
        if (primaryLabel != null)
        {
            primaryLabel.text = label;
            if (uiFont != null) primaryLabel.font = uiFont;
        }
    }

    private void RefreshHearts()
    {
        if (hearts == null) return;

        for (int i = 0; i < hearts.childCount; i++)
        {
            hearts.GetChild(i).gameObject.SetActive(i < hpValue);
        }
    }

    private void OnPrimaryClicked()
    {
        if (levelManager == null) return;

        switch (resultAction)
        {
            case ResultAction.NextLevel:
                levelManager.NextLevel();
                break;
            case ResultAction.Retry:
                levelManager.RestartCurrentLevel();
                break;
            case ResultAction.RestartGame:
                levelManager.RestartGame();
                break;
        }
    }

    private void OnExitClicked()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL 不能真正退出标签页，按需求打开项目主页。
        Application.OpenURL("https://66yanggg.github.io");
#else
        Application.Quit();

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
#endif
    }
}
