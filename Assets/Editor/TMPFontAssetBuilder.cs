#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// 生成游戏专用的中文 TMP 字体资产（Static 图集）。
/// 字体来源为项目内 Assets/FontsSource/NotoSansSC-Regular.otf（SIL OFL）。
/// 生成后的字体资产放在 Assets/Resources/Fonts/GameChineseFont.asset，
/// 运行时通过 Resources.Load 加载，WebGL 和 Windows 使用同一份图集。
/// </summary>
[InitializeOnLoad]
public static class TMPFontAssetBuilder
{
    private const string FontSourcePath = "Assets/FontsSource/NotoSansSC-Regular.otf";
    private const string FontAssetPath = "Assets/Resources/Fonts/GameChineseFont.asset";
    private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
    private const string SetupMarkerName = "TMP_FONT_SETUP_ON";
    private const string ResultFileName = "TMP_FONT_SETUP_RESULT.txt";

    private static double nextPollTime;

    private static string ProjectRoot
    {
        get { return Directory.GetParent(Application.dataPath).FullName; }
    }

    private static string SetupMarker
    {
        get { return Path.Combine(ProjectRoot, SetupMarkerName); }
    }

    private static string ResultFile
    {
        get { return Path.Combine(ProjectRoot, ResultFileName); }
    }

    static TMPFontAssetBuilder()
    {
        if (Application.isPlaying || !File.Exists(SetupMarker)) return;
        EditorApplication.update += Tick;
    }

    [MenuItem("Tools/TextMeshPro/重建游戏中文 TMP 字体")]
    public static void BuildFromMenu()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("TMP 字体", "请先退出 Play 模式再生成字体。", "确定");
            return;
        }

        if (!EnsureTmpEssentialsForMenu()) return;
        try
        {
            GenerateFont();
        }
        catch (Exception exception)
        {
            WriteResult("FAIL", exception.ToString());
            Debug.LogException(exception);
        }
    }

    private static void Tick()
    {
        if (Application.isPlaying) return;

        if (!File.Exists(SetupMarker))
        {
            EditorApplication.update -= Tick;
            return;
        }

        if (EditorApplication.timeSinceStartup < nextPollTime) return;
        nextPollTime = EditorApplication.timeSinceStartup + 0.5;

        if (!TmpEssentialsReady())
        {
            TryImportTmpEssentials();
            return;
        }

        Font font = AssetDatabase.LoadAssetAtPath<Font>(FontSourcePath);
        if (font == null) return;

        EditorApplication.update -= Tick;

        try
        {
            GenerateFont(font);
        }
        catch (Exception exception)
        {
            WriteResult("FAIL", exception.ToString());
            Debug.LogException(exception);
        }
    }

    private static bool TmpEssentialsReady()
    {
        if (!File.Exists(Path.Combine(ProjectRoot, TmpSettingsPath))) return false;
        return Shader.Find("TextMeshPro/Mobile/Distance Field") != null;
    }

    private static bool EnsureTmpEssentialsForMenu()
    {
        if (TmpEssentialsReady()) return true;
        TryImportTmpEssentials();
        EditorUtility.DisplayDialog("TMP 字体",
            "正在导入 TMP Essential Resources。导入完成后请再次点击菜单生成字体。", "确定");
        return false;
    }

    private static void TryImportTmpEssentials()
    {
        if (SessionState.GetBool("TMPFontSetup_Importing", false)) return;
        SessionState.SetBool("TMPFontSetup_Importing", true);

        try
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForPackageName("com.unity.textmeshpro");
            if (package == null)
            {
                WriteResult("FAIL", "找不到 com.unity.textmeshpro 包。");
                return;
            }

            string packagePath = Path.Combine(package.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
            if (!File.Exists(packagePath))
            {
                WriteResult("FAIL", "找不到 TMP Essential Resources：" + packagePath);
                return;
            }

            Debug.Log("[TMPFont] 导入 TMP Essential Resources: " + packagePath);
            AssetDatabase.ImportPackage(packagePath, false);
        }
        catch (Exception exception)
        {
            WriteResult("FAIL", "导入 TMP Essential Resources 失败：" + exception);
            Debug.LogException(exception);
        }
    }

    private static void GenerateFont(Font explicitFont = null)
    {
        Font font = explicitFont != null ? explicitFont : AssetDatabase.LoadAssetAtPath<Font>(FontSourcePath);
        if (font == null)
        {
            WriteResult("FAIL", "找不到字体源文件：" + FontSourcePath);
            return;
        }

        TrueTypeFontImporter importer = AssetImporter.GetAtPath(FontSourcePath) as TrueTypeFontImporter;
        if (importer != null && !importer.includeFontData)
        {
            importer.includeFontData = true;
            importer.SaveAndReimport();
            font = AssetDatabase.LoadAssetAtPath<Font>(FontSourcePath);
        }

        string characters = BuildCharacterSet();
        string required = RequiredCharacters();

        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath) != null)
        {
            AssetDatabase.DeleteAsset(FontAssetPath);
        }

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            font,
            64,
            6,
            GlyphRenderMode.SDFAA,
            2048,
            2048,
            AtlasPopulationMode.Dynamic,
            true);

        if (fontAsset == null)
        {
            WriteResult("FAIL", "TMP_FontAsset.CreateFontAsset 返回 null，检查 TMP Essential 资源和字体导入设置。");
            return;
        }

        fontAsset.name = "GameChineseFont";

        bool added = fontAsset.TryAddCharacters(characters, out string missingCharacters, true);
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
        fontAsset.ReadFontAssetDefinition();

        List<char> missingRequired = new List<char>();
        for (int i = 0; i < required.Length; i++)
        {
            if (!fontAsset.HasCharacter(required[i]) && !missingRequired.Contains(required[i]))
            {
                missingRequired.Add(required[i]);
            }
        }

        string directory = Path.GetDirectoryName(FontAssetPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            AssetDatabase.Refresh();
        }

        AssetDatabase.CreateAsset(fontAsset, FontAssetPath);

        for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
        {
            Texture2D texture = fontAsset.atlasTextures[i];
            if (texture == null) continue;
            texture.name = fontAsset.name + " Atlas " + i;
            if (!AssetDatabase.IsSubAsset(texture))
            {
                AssetDatabase.AddObjectToAsset(texture, fontAsset);
            }
        }

        if (fontAsset.material != null)
        {
            fontAsset.material.name = fontAsset.name + " Material";
            if (!AssetDatabase.IsSubAsset(fontAsset.material))
            {
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }
        }

        SetDefaultTmpFont(fontAsset);

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        StringBuilder result = new StringBuilder();
        result.AppendLine(missingRequired.Count == 0 ? "PASS" : "FAIL");
        result.AppendLine("字体资产：" + FontAssetPath);
        result.AppendLine("字符总数：" + characters.Length);
        result.AppendLine("图集数量：" + fontAsset.atlasTextures.Length);
        result.AppendLine("图集尺寸：2048 x 2048");
        result.AppendLine("动态添加结果：" + added);
        result.AppendLine("字体中缺失字符：" + (string.IsNullOrEmpty(missingCharacters) ? "无" : missingCharacters));
        result.AppendLine("UI 必需字符缺失：" + (missingRequired.Count == 0 ? "无" : new string(missingRequired.ToArray())));

        WriteResult(missingRequired.Count == 0 ? "PASS" : "FAIL", result.ToString());

        if (File.Exists(SetupMarker)) File.Delete(SetupMarker);
        Selection.activeObject = fontAsset;
        EditorGUIUtility.PingObject(fontAsset);
        Debug.Log("[TMPFont] 字体生成完成：" + FontAssetPath + "，图集数：" + fontAsset.atlasTextures.Length);
    }

    private static void SetDefaultTmpFont(TMP_FontAsset fontAsset)
    {
        TMP_Settings settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
        if (settings == null) return;

        SerializedObject serialized = new SerializedObject(settings);
        SerializedProperty property = serialized.FindProperty("m_defaultFontAsset");
        if (property != null)
        {
            property.objectReferenceValue = fontAsset;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
        }
    }

    private static string BuildCharacterSet()
    {
        HashSet<char> characters = new HashSet<char>();

        for (char c = ' '; c <= '~'; c++)
        {
            characters.Add(c);
        }

        AddCharacters(characters,
            "牧场动物大作战第关击杀得分准备下一全部通关游戏结束重试本再玩一次退出最终失败胜利" +
            "热身鸡飞狗跳森林来客猎手试炼终极围猎或移动按住空格连续射击重新开始打开主页左右" +
            "分数主页网页" +
            "，。！？：；、（）【】《》“”‘’·—…￥×÷←→↑↓◀▶●○■□★☆");

        string assetsRoot = Path.Combine(ProjectRoot, "Assets");
        if (Directory.Exists(assetsRoot))
        {
            foreach (string file in Directory.GetFiles(assetsRoot, "*.cs", SearchOption.AllDirectories))
            {
                string text = File.ReadAllText(file, Encoding.UTF8);
                for (int i = 0; i < text.Length; i++)
                {
                    char c = text[i];
                    if (IsWantedCharacter(c)) characters.Add(c);
                }
            }
        }

        return new string(characters.OrderBy(c => c).ToArray());
    }

    private static string RequiredCharacters()
    {
        return "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz" +
               "第关击杀得分准备下一全部通关游戏结束重试本再玩一次退出最终失败胜利" +
               "牧场动物大作战热身鸡飞狗跳森林来客猎手试炼终极围猎" +
               "或移动按住空格连续射击重新开始打开主页左右射击" +
               "，。！？：；、（）【】《》“”‘’·—…￥×÷←→↑↓";
    }

    private static void AddCharacters(HashSet<char> target, string value)
    {
        for (int i = 0; i < value.Length; i++)
        {
            if (!char.IsControl(value[i])) target.Add(value[i]);
        }
    }

    private static bool IsWantedCharacter(char c)
    {
        if (c >= ' ' && c <= '~') return true;
        int code = c;
        return (code >= 0x00A0 && code <= 0x00FF)
            || (code >= 0x2000 && code <= 0x206F)
            || (code >= 0x2190 && code <= 0x21FF)
            || (code >= 0x25A0 && code <= 0x25FF)
            || (code >= 0x3000 && code <= 0x303F)
            || (code >= 0x4E00 && code <= 0x9FFF)
            || (code >= 0xFF00 && code <= 0xFFEF);
    }

    private static void WriteResult(string status, string message)
    {
        try
        {
            File.WriteAllText(ResultFile, status + "\n" + message, Encoding.UTF8);
        }
        catch (Exception exception)
        {
            Debug.LogError("[TMPFont] 写入结果失败：" + exception);
        }

        Debug.Log("[TMPFont] " + status + "\n" + message);
    }
}
#endif
