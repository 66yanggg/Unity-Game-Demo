#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// 把当前项目构建为 WebGL，并输出到 GitHub Pages 使用的 docs 目录。
/// 菜单：Tools/WebGL/构建网页版到 docs
/// 也支持通过项目根目录的 WEBGL_BUILD_ON 标记自动执行。
/// </summary>
[InitializeOnLoad]
public static class WebGLBuilder
{
    private const string ProductName = "牧场动物大作战";
    private const string BuildMarkerName = "WEBGL_BUILD_ON";
    private const string StateFileName = "WEBGL_BUILD_STATE.txt";
    private const string ResultFileName = "WEBGL_BUILD_RESULT.txt";
    private const string FontAssetPath = "Assets/Resources/Fonts/GameChineseFont.asset";

    private static double nextPoll;

    private static string ProjectRoot
    {
        get { return Directory.GetParent(Application.dataPath).FullName; }
    }

    private static string BuildMarker
    {
        get { return Path.Combine(ProjectRoot, BuildMarkerName); }
    }

    private static string StateFile
    {
        get { return Path.Combine(ProjectRoot, StateFileName); }
    }

    private static string ResultFile
    {
        get { return Path.Combine(ProjectRoot, ResultFileName); }
    }

    static WebGLBuilder()
    {
        if (Application.isPlaying || !File.Exists(BuildMarker)) return;
        EditorApplication.update += Tick;
    }

    [MenuItem("Tools/WebGL/构建网页版到 docs")]
    public static void BuildFromMenu()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("WebGL 构建", "请先退出 Play 模式。", "确定");
            return;
        }

        if (!File.Exists(Path.Combine(ProjectRoot, FontAssetPath)))
        {
            EditorUtility.DisplayDialog("WebGL 构建", "找不到 TMP 中文字体，请先生成字体。", "确定");
            return;
        }

        PrepareSettings();
        BuildToDocs();
    }

    private static void Tick()
    {
        if (Application.isPlaying) return;
        if (!File.Exists(BuildMarker))
        {
            EditorApplication.update -= Tick;
            return;
        }

        if (EditorApplication.timeSinceStartup < nextPoll) return;
        nextPoll = EditorApplication.timeSinceStartup + 1.0;

        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

        if (!File.Exists(Path.Combine(ProjectRoot, FontAssetPath)))
        {
            WriteResult("FAIL", "找不到字体资产：" + FontAssetPath);
            return;
        }

        PrepareSettings();

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
        {
            Debug.Log("[WebGLBuilder] 切换到 WebGL 平台...");
            bool switched = EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            if (!switched)
            {
                WriteResult("FAIL", "切换 WebGL 构建平台失败。");
            }
            return;
        }

        if (File.Exists(StateFile) && File.ReadAllText(StateFile).Trim() == "building")
        {
            return;
        }

        BuildToDocs();
    }

    private static void PrepareSettings()
    {
        PlayerSettings.productName = ProductName;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.template = "APPLICATION:Default";
        AssetDatabase.SaveAssets();
        Debug.Log("[WebGLBuilder] Product Name = " + PlayerSettings.productName);
    }

    private static void BuildToDocs()
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            WriteResult("FAIL", "Build Settings 中没有启用任何场景。");
            return;
        }

        string outputPath = Path.Combine(ProjectRoot, "docs");
        Directory.CreateDirectory(outputPath);
        File.WriteAllText(StateFile, "building");

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        Debug.Log("[WebGLBuilder] 开始构建：" + outputPath);
        BuildReport report = BuildPipeline.BuildPlayer(options);

        if (report == null)
        {
            WriteResult("FAIL", "BuildPipeline.BuildPlayer 返回 null。");
            return;
        }

        if (report.summary.result != BuildResult.Succeeded)
        {
            WriteResult("FAIL", "构建结果：" + report.summary.result +
                               "，错误数：" + report.summary.totalErrors);
            return;
        }

        // GitHub Pages 默认使用 Jekyll，加 .nojekyll 避免以下划线开头的文件被忽略。
        File.WriteAllText(Path.Combine(outputPath, ".nojekyll"), string.Empty);
        PatchMobileCss(outputPath);

        WriteResult("PASS",
            "构建成功\n" +
            "输出目录：" + outputPath + "\n" +
            "总大小：" + report.summary.totalSize + " bytes\n" +
            "Product Name：" + PlayerSettings.productName);

        if (File.Exists(StateFile)) File.Delete(StateFile);
        if (File.Exists(BuildMarker)) File.Delete(BuildMarker);
        EditorUtility.RevealInFinder(outputPath);
    }

    /// <summary>
    /// 给默认 WebGL 模板补上移动端样式，防止手指拖动时浏览器滚动/缩放页面。
    /// </summary>
    private static void PatchMobileCss(string outputPath)
    {
        string cssPath = Path.Combine(outputPath, "TemplateData", "style.css");
        if (!File.Exists(cssPath)) return;

        string css = File.ReadAllText(cssPath);
        if (css.Contains("touch-action")) return;

        css += "\n/* Mobile: prevent browser scrolling/zoom while using the game canvas */\n" +
               "body { overscroll-behavior: none; }\n" +
               "#unity-container, #unity-canvas, .unity-mobile #unity-canvas { touch-action: none; -ms-touch-action: none; }\n";
        File.WriteAllText(cssPath, css);
    }

    private static void WriteResult(string status, string message)
    {
        try
        {
            File.WriteAllText(ResultFile, status + "\n" + message + "\n");
        }
        catch (Exception exception)
        {
            Debug.LogError("[WebGLBuilder] 写入结果失败：" + exception);
        }

        if (File.Exists(StateFile)) File.Delete(StateFile);
        if (File.Exists(BuildMarker)) File.Delete(BuildMarker);
        EditorApplication.update -= Tick;
        Debug.Log("[WebGLBuilder] " + status + "\n" + message);
    }
}
#endif
