using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 游戏 UI 与状态控制：玩家血量、分数、关卡 HUD、过关/失败结算。
/// 出怪与关卡数值由 LevelManager 负责，这里不直接处理生成逻辑。
/// </summary>
public class GameController : MonoBehaviour
{
    private Transform hearts;
    private Text scoreText;
    private Transform endPanel;
    private Image resultImage;
    private Text resultText;
    private Text resultSubText;
    private Button primaryButton;
    private Button exitButton;
    private Text primaryLabel;
    private Text exitLabel;

    private GameObject hudRoot;
    private Text levelText;
    private Text targetText;
    private Text hintText;
    private Font uiFont;

    private LevelManager levelManager;
    private Playercontroller playerController;

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
        if (!CacheSceneUi())
        {
            enabled = false;
            return;
        }

        BuildHud();

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

    private bool CacheSceneUi()
    {
        hearts = transform.Find("HpImg");
        Transform scoreRoot = transform.Find("ScoreText/score");
        scoreText = scoreRoot != null ? scoreRoot.GetComponent<Text>() : null;
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

        primaryLabel = primaryButton.GetComponentInChildren<Text>(true);
        exitLabel = exitButton.GetComponentInChildren<Text>(true);

        primaryButton.onClick.RemoveAllListeners();
        primaryButton.onClick.AddListener(OnPrimaryClicked);
        exitButton.onClick.RemoveAllListeners();
        exitButton.onClick.AddListener(OnExitClicked);

        playerController = FindObjectOfType<Playercontroller>();

        // 原分数文字是深灰色，这里加白字和描边，保证在草地背景上清晰。
        Text scoreLabel = scoreText.transform.parent.GetComponent<Text>();
        if (scoreLabel != null)
        {
            scoreLabel.color = Color.white;
            AddOutline(scoreLabel);
        }
        scoreText.color = Color.white;
        AddOutline(scoreText);

        resultImage.preserveAspect = true;
        return true;
    }

    private void BuildHud()
    {
        uiFont = ResolveFont();

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
            new Vector2(1000f, 46f), 32, TextAnchor.MiddleCenter);

        targetText = CreateText(
            "TargetText", hudObject.transform,
            new Vector2(0.5f, 1f), new Vector2(0f, -72f),
            new Vector2(800f, 34f), 22, TextAnchor.MiddleCenter);

        hintText = CreateText(
            "HintText", hudObject.transform,
            new Vector2(0.5f, 0f), new Vector2(0f, 26f),
            new Vector2(1000f, 30f), 20, TextAnchor.MiddleCenter);
        hintText.color = new Color(1f, 1f, 1f, 0.8f);
        hintText.text = "A / D 或 ← → 移动   ·   按住空格连续射击";

        resultText = CreateText(
            "ResultTitle", endPanel,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 168f),
            new Vector2(1100f, 84f), 46, TextAnchor.MiddleCenter);

        resultSubText = CreateText(
            "ResultSub", endPanel,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 104f),
            new Vector2(1000f, 42f), 26, TextAnchor.MiddleCenter);
        resultSubText.color = new Color(1f, 0.95f, 0.75f);

        if (primaryLabel != null && uiFont != null) primaryLabel.font = uiFont;
        if (exitLabel != null && uiFont != null) exitLabel.font = uiFont;
    }

    private Text CreateText(
        string name,
        Transform parent,
        Vector2 anchor,
        Vector2 anchoredPosition,
        Vector2 size,
        int fontSize,
        TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Text text = textObject.GetComponent<Text>();
        if (uiFont != null) text.font = uiFont;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        AddOutline(text);
        return text;
    }

    private Font ResolveFont()
    {
        try
        {
            Font font = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 28);
            if (font != null) return font;
        }
        catch (Exception)
        {
            // 某些平台没有该字体，继续走后备方案。
        }

        try
        {
            Font font = Font.CreateDynamicFontFromOSFont("Arial", 28);
            if (font != null) return font;
        }
        catch (Exception)
        {
            // 忽略并使用内置字体。
        }

        if (scoreText != null && scoreText.font != null)
        {
            return scoreText.font;
        }

        Font builtin = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (builtin == null)
        {
            builtin = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        return builtin;
    }

    private static void AddOutline(Text text)
    {
        if (text == null || text.GetComponent<Outline>() != null) return;

        Outline outline = text.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
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
            GameFacade.Instance.PlaySound("x_AudioResources/胜利音效", 0.9f);
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
            GameFacade.Instance.PlaySound("x_AudioResources/失败音效", 0.9f);
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
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
