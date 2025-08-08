using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using System.Collections.Generic;

[InitializeOnLoad]
public static class TimeWasterTracker
{
    private static DateTime compileStart;
    private static DateTime domainReloadStart;
    private static double totalCompileTime;
    private static double totalDomainReloadTime;

    private const string COMPILE_KEY = "TimeWasterTracker_Compile";
    private const string DOMAIN_KEY = "TimeWasterTracker_Domain";

    private const float COMPILE_WARNING_THRESHOLD = 15.0f;  // seconds
    private const float DOMAIN_WARNING_THRESHOLD = 20.0f;  // seconds
    private static double lastCompileDuration = 0;
    private static double lastDomainReloadDuration = 0;
    private static DateTime lastPopupTime = DateTime.MinValue;

    private const string OVERLAY_ENABLED_KEY = "TimeWasterTracker_OverlayEnabled";
    private const string NOTIFICATIONS_ENABLED_KEY = "TimeWasterTracker_NotificationsEnabled";
    private const string AUTO_ANALYZE_KEY = "TimeWasterTracker_AutoAnalyze";

    public static bool OverlayEnabled
    {
        get => EditorPrefs.GetBool(OVERLAY_ENABLED_KEY, true);
        set => EditorPrefs.SetBool(OVERLAY_ENABLED_KEY, value);
    }

    public static bool NotificationsEnabled
    {
        get => EditorPrefs.GetBool(NOTIFICATIONS_ENABLED_KEY, true);
        set => EditorPrefs.SetBool(NOTIFICATIONS_ENABLED_KEY, value);
    }

    public static bool AutoAnalyzeEnabled
    {
        get => EditorPrefs.GetBool(AUTO_ANALYZE_KEY, true);
        set => EditorPrefs.SetBool(AUTO_ANALYZE_KEY, value);
    }

    // Performance analysis data
    private static List<string> longestCompiles = new List<string>();
    private static List<string> longestReloads = new List<string>();
    private static Dictionary<string, double> assemblyCompileTimes = new Dictionary<string, double>();

    static TimeWasterTracker()
    {
        // Load saved data
        totalCompileTime = EditorPrefs.GetFloat(COMPILE_KEY, 0);
        totalDomainReloadTime = EditorPrefs.GetFloat(DOMAIN_KEY, 0);

        // Domain reload tracking
        domainReloadStart = DateTime.Now;
        EditorApplication.delayCall += OnDomainReloadComplete;

        // Compilation tracking
        CompilationPipeline.compilationStarted += OnCompileStarted;
        CompilationPipeline.compilationFinished += OnCompileFinished;

        // Assembly-specific tracking
        AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;

        UpdateOverlaySubscription();
        EditorApplication.quitting += SaveTimes;
    }

    private static void OnDomainReloadComplete()
    {
        var domainReloadTime = (DateTime.Now - domainReloadStart).TotalSeconds;
        lastDomainReloadDuration = domainReloadTime;
        totalDomainReloadTime += domainReloadTime;
        EditorPrefs.SetFloat(DOMAIN_KEY, (float)totalDomainReloadTime);
        TimeWasterHistory.AddDomain((float)domainReloadTime);

        if (domainReloadTime > DOMAIN_WARNING_THRESHOLD)
        {
            Debug.LogWarning($"⚠️ Domain Reload took {domainReloadTime:F2} seconds — exceeds {DOMAIN_WARNING_THRESHOLD}s.");
            if (NotificationsEnabled) TryShowPopup("Domain Reload", domainReloadTime);

            // Record long reload
            longestReloads.Add($"{DateTime.Now:HH:mm} - {domainReloadTime:F2}s");
            if (longestReloads.Count > 5) longestReloads.RemoveAt(0);
        }

        // Spike detection
        var avg = TimeWasterHistory.DomainHistory.Count > 0
            ? TimeWasterHistory.DomainHistory.Average()
            : 0;

        if (avg > 0 && domainReloadTime > avg * 2)
        {
            Debug.Log($"🔥 Spike Detected: Domain Reload took {domainReloadTime:F2}s (average: {avg:F2}s)");
        }

        if (AutoAnalyzeEnabled && domainReloadTime > DOMAIN_WARNING_THRESHOLD)
        {
            AnalyzeDomainReload(domainReloadTime);
        }
    }

    private static void OnCompileStarted(object obj)
    {
        compileStart = DateTime.Now;
        assemblyCompileTimes.Clear();
    }

    private static void OnCompileFinished(object obj)
    {
        var duration = (DateTime.Now - compileStart).TotalSeconds;
        lastCompileDuration = duration;
        totalCompileTime += duration;
        EditorPrefs.SetFloat(COMPILE_KEY, (float)totalCompileTime);
        TimeWasterHistory.AddCompile((float)duration);

        if (duration > COMPILE_WARNING_THRESHOLD)
        {
            Debug.LogWarning($"⚠️ Compilation took {duration:F2} seconds — exceeds {COMPILE_WARNING_THRESHOLD}s.");
            if (NotificationsEnabled) TryShowPopup("Compilation", duration);

            // Record long compile
            longestCompiles.Add($"{DateTime.Now:HH:mm} - {duration:F2}s");
            if (longestCompiles.Count > 5) longestCompiles.RemoveAt(0);
        }

        // Spike detection
        var avg = TimeWasterHistory.CompileHistory.Count > 0
            ? TimeWasterHistory.CompileHistory.Average()
            : 0;

        if (avg > 0 && duration > avg * 2)
        {
            Debug.Log($"🔥 Spike Detected: Compile took {duration:F2}s (average: {avg:F2}s)");
        }

        if (AutoAnalyzeEnabled && duration > COMPILE_WARNING_THRESHOLD)
        {
            AnalyzeCompileTime(duration);
        }
    }

    private static void OnBeforeAssemblyReload()
    {
        // Track which assemblies are being reloaded
    }

    private static void OnAfterAssemblyReload()
    {
        // Analyze assembly reload impact
    }

    private static void AnalyzeCompileTime(double duration)
    {
        var assemblies = CompilationPipeline.GetAssemblies()
            .OrderByDescending(a => assemblyCompileTimes.ContainsKey(a.name) ? assemblyCompileTimes[a.name] : 0)
            .Take(3)
            .ToArray();

        if (assemblies.Length > 0)
        {
            string topAssemblies = string.Join(", ", assemblies.Select(a => a.name));
            Debug.Log($"🔍 Top assemblies by compile time: {topAssemblies}");
        }

        // Check for common performance issues
        if (duration > 30)
        {
            Debug.LogError("🚨 Extremely long compile detected! Possible issues:");
            Debug.LogError("- Large third-party DLLs in Assets folder");
            Debug.LogError("- Excessive use of #if UNITY_EDITOR directives");
            Debug.LogError("- Very large source files (>10k lines)");
        }
        else if (duration > 15)
        {
            Debug.LogWarning("⚠️ Long compile detected. Consider:");
            Debug.LogWarning("- Moving editor-only code to separate assemblies");
            Debug.LogWarning("- Using Assembly Definition Files to split your code");
            Debug.LogWarning("- Reviewing large scripts");
        }
    }

    private static void AnalyzeDomainReload(double duration)
    {
        Debug.Log("🔍 Analyzing domain reload...");

        // Check for common performance issues
        if (duration > 30)
        {
            Debug.LogError("🚨 Extremely long domain reload detected! Possible issues:");
            Debug.LogError("- Static constructors doing heavy work");
            Debug.LogError("- [InitializeOnLoad] methods with expensive operations");
            Debug.LogError("- Large ScriptableObject assets in memory");
        }
        else if (duration > 15)
        {
            Debug.LogWarning("⚠️ Long domain reload detected. Consider:");
            Debug.LogWarning("- Using Editor-only assemblies to reduce reload impact");
            Debug.LogWarning("- Reviewing static initializers and [InitializeOnLoad] methods");
            Debug.LogWarning("- Disabling Domain Reload in Play Mode Settings for testing");
        }
    }

    private static void SaveTimes()
    {
        EditorPrefs.SetFloat(COMPILE_KEY, (float)totalCompileTime);
        EditorPrefs.SetFloat(DOMAIN_KEY, (float)totalDomainReloadTime);
    }

    private static void DrawOverlay(SceneView sceneView)
    {
        if (!OverlayEnabled || EditorApplication.isCompiling)
            return;

        Handles.BeginGUI();

        double total = totalCompileTime + totalDomainReloadTime;
        TimeSpan span = TimeSpan.FromSeconds(total);

        string label =
            $"❤️ Life wasted: {span:hh\\:mm\\:ss}\n" +
            $"⏱️ Last: {lastCompileDuration:F1}s compile, {lastDomainReloadDuration:F1}s reload";

        GUIStyle style = new GUIStyle(EditorStyles.label)
        {
            normal = { textColor = Color.gray },
            alignment = TextAnchor.UpperLeft,
            fontSize = 11,
            richText = true
        };

        GUILayout.BeginArea(new Rect(10, 10, 250, 60));
        GUILayout.Label(label, style);

        // Add performance tips if recent operation was slow
        if (lastCompileDuration > COMPILE_WARNING_THRESHOLD * 2 ||
            lastDomainReloadDuration > DOMAIN_WARNING_THRESHOLD * 2)
        {
            GUILayout.Label("<color=yellow>⚠️ Check Time Waster window for tips</color>", style);
        }

        GUILayout.EndArea();

        Handles.EndGUI();
    }

    private static void TryShowPopup(string type, double time)
    {
        if ((DateTime.Now - lastPopupTime).TotalSeconds < 30)
            return;

        lastPopupTime = DateTime.Now;

        EditorApplication.delayCall += () =>
        {
            int option = EditorUtility.DisplayDialogComplex(
                "⚠️ Slow Operation Detected",
                $"{type} took {time:F2} seconds.\n\nWould you like to see optimization tips?",
                "Yes", "No", "Don't Show Again");

            switch (option)
            {
                case 0: // Yes
                    TimeWasterWindow.ShowOptimizationTips(type);
                    break;
                case 1: // No
                    break;
                case 2: // Don't Show Again
                    NotificationsEnabled = false;
                    break;
            }
        };
    }

    public static void UpdateOverlaySubscription()
    {
        SceneView.duringSceneGui -= DrawOverlay;
        if (OverlayEnabled)
        {
            SceneView.duringSceneGui += DrawOverlay;
        }
    }

    public static float GetCompileTime() => (float)totalCompileTime;
    public static float GetDomainReloadTime() => (float)totalDomainReloadTime;
    public static List<string> GetLongestCompiles() => longestCompiles;
    public static List<string> GetLongestReloads() => longestReloads;

    public static void Reset()
    {
        totalCompileTime = 0;
        totalDomainReloadTime = 0;
        longestCompiles.Clear();
        longestReloads.Clear();
        EditorPrefs.SetFloat(COMPILE_KEY, 0);
        EditorPrefs.SetFloat(DOMAIN_KEY, 0);
    }
}