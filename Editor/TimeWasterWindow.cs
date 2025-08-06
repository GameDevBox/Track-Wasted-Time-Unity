using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;
using System.Linq;

public class TimeWasterWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private bool showTips = true;
    private bool showHistory = true;
    private bool showSettings = true;
    private bool showPerformance = true;

    [MenuItem("Window/Time Waster Stats")]
    public static void ShowWindow()
    {
        var window = GetWindow<TimeWasterWindow>("Time Waster Stats");
        window.minSize = new Vector2(400, 500);
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawSummarySection();
        DrawBreakdownSection();
        DrawHistoryGraphs();
        DrawPerformanceAnalysis();
        DrawSettingsSection();
        DrawOptimizationTips();

        EditorGUILayout.EndScrollView();
    }

    private void DrawSummarySection()
    {
        float compile = TimeWasterTracker.GetCompileTime();
        float domain = TimeWasterTracker.GetDomainReloadTime();
        float total = compile + domain;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Unity Time Waster Tracker", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField($"Total time wasted: {FormatTime(total)}", EditorStyles.largeLabel);
        EditorGUILayout.Space();
    }

    private void DrawBreakdownSection()
    {
        float compile = TimeWasterTracker.GetCompileTime();
        float domain = TimeWasterTracker.GetDomainReloadTime();
        float total = compile + domain;

        EditorGUILayout.LabelField("Breakdown by Category:", EditorStyles.boldLabel);

        float compilePercent = total > 0 ? compile / total : 0;
        float domainPercent = total > 0 ? domain / total : 0;

        DrawStat("Compilation", compile, compilePercent, new Color(0.3f, 0.8f, 1f));
        DrawStat("Domain Reload", domain, domainPercent, new Color(1f, 0.5f, 0.5f));

        EditorGUILayout.Space();
    }

    private void DrawHistoryGraphs()
    {
        showHistory = EditorGUILayout.Foldout(showHistory, "Performance History", true);
        if (showHistory)
        {
            EditorGUILayout.Space();
            DrawHistoryGraph(TimeWasterHistory.CompileHistory, new Color(0.3f, 0.8f, 1f), "Compile Time History");
            EditorGUILayout.Space();
            DrawHistoryGraph(TimeWasterHistory.DomainHistory, new Color(1f, 0.5f, 0.5f), "Domain Reload History");
            EditorGUILayout.Space();
        }
    }

    private void DrawPerformanceAnalysis()
    {
        showPerformance = EditorGUILayout.Foldout(showPerformance, "Performance Analysis", true);
        if (showPerformance)
        {
            EditorGUILayout.Space();

            // Longest compiles
            EditorGUILayout.LabelField("Longest Compiles:", EditorStyles.boldLabel);
            var compiles = TimeWasterTracker.GetLongestCompiles();
            if (compiles.Count == 0)
            {
                EditorGUILayout.LabelField("No slow compiles recorded");
            }
            else
            {
                foreach (var entry in compiles)
                {
                    EditorGUILayout.LabelField(entry);
                }
            }

            EditorGUILayout.Space();

            // Longest reloads
            EditorGUILayout.LabelField("Longest Domain Reloads:", EditorStyles.boldLabel);
            var reloads = TimeWasterTracker.GetLongestReloads();
            if (reloads.Count == 0)
            {
                EditorGUILayout.LabelField("No slow reloads recorded");
            }
            else
            {
                foreach (var entry in reloads)
                {
                    EditorGUILayout.LabelField(entry);
                }
            }

            EditorGUILayout.Space();
        }
    }

    private void DrawSettingsSection()
    {
        showSettings = EditorGUILayout.Foldout(showSettings, "Settings", true);
        if (showSettings)
        {
            EditorGUILayout.Space();

            // Overlay toggle
            bool overlayEnabled = TimeWasterTracker.OverlayEnabled;
            bool newOverlayValue = EditorGUILayout.Toggle("Show Overlay in SceneView", overlayEnabled);
            if (newOverlayValue != overlayEnabled)
            {
                TimeWasterTracker.OverlayEnabled = newOverlayValue;
                TimeWasterTracker.UpdateOverlaySubscription();
            }

            // Notifications toggle
            bool notificationsEnabled = TimeWasterTracker.NotificationsEnabled;
            bool newNotificationsValue = EditorGUILayout.Toggle("Enable Notifications", notificationsEnabled);
            if (newNotificationsValue != notificationsEnabled)
            {
                TimeWasterTracker.NotificationsEnabled = newNotificationsValue;
            }

            // Auto-analyze toggle
            bool autoAnalyzeEnabled = TimeWasterTracker.AutoAnalyzeEnabled;
            bool newAutoAnalyzeValue = EditorGUILayout.Toggle("Auto-Analyze Slow Operations", autoAnalyzeEnabled);
            if (newAutoAnalyzeValue != autoAnalyzeEnabled)
            {
                TimeWasterTracker.AutoAnalyzeEnabled = newAutoAnalyzeValue;
            }

            EditorGUILayout.Space();

            // Action buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh Data")) Repaint();
            if (GUILayout.Button("Reset All Data"))
            {
                if (EditorUtility.DisplayDialog("Reset All Data",
                    "Are you sure you want to reset all collected time data?",
                    "Yes", "No"))
                {
                    TimeWasterTracker.Reset();
                    TimeWasterHistory.Reset();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }
    }

    private void DrawOptimizationTips()
    {
        showTips = EditorGUILayout.Foldout(showTips, "Optimization Tips", true);
        if (showTips)
        {
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("General Tips:", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "1. Use Assembly Definition Files to split your code into smaller assemblies\n" +
                "2. Move editor-only code to separate assemblies marked with Editor-only compilation\n" +
                "3. Avoid expensive operations in static constructors and [InitializeOnLoad] methods\n" +
                "4. Consider disabling Domain Reload in Play Mode Settings during development\n" + 
                "5. Restarting Project after couple of hours working on it (Recommended)",
                MessageType.Info);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Compilation Tips:", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "1. Avoid large source files (>2000 lines)\n" +
                "2. Minimize use of #if UNITY_EDITOR directives\n" +
                "3. Place third-party DLLs in Plugins folder instead of Assets\n" +
                "4. Review generated code (e.g., serialization code)",
                MessageType.Info);

            EditorGUILayout.Space();

            if (GUILayout.Button("More Optimization Resources"))
            {
                Application.OpenURL("https://docs.unity3d.com/6000.1/Documentation/Manual/compilation-and-code-reload.html");
            }

            EditorGUILayout.Space();
        }
    }

    public static void ShowOptimizationTips(string type)
    {
        var window = GetWindow<TimeWasterWindow>("Time Waster Stats");
        window.showTips = true;
        window.Focus();
    }

    private void DrawStat(string label, float seconds, float percent, Color color)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"{label}: {FormatTime(seconds)} ({percent * 100:F1}%)");
        EditorGUILayout.EndHorizontal();

        Rect r = GUILayoutUtility.GetRect(100, 8);
        EditorGUI.DrawRect(new Rect(r.x, r.y, r.width * percent, r.height), color);
    }

    private void DrawHistoryGraph(List<float> data, Color color, string label)
    {
        if (data.Count == 0)
        {
            EditorGUILayout.LabelField($"{label}: No data available");
            return;
        }

        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

        // Calculate stats
        float max = Mathf.Max(1f, data.Max());
        float min = data.Min();
        float avg = data.Average();
        float last = data.Last();

        EditorGUILayout.LabelField($"Last: {last:F2}s | Avg: {avg:F2}s | Max: {max:F2}s | Min: {min:F2}s");

        // Draw graph
        Rect r = GUILayoutUtility.GetRect(300, 60);
        EditorGUI.DrawRect(r, new Color(0.15f, 0.15f, 0.15f));

        float barWidth = r.width / data.Count;

        for (int i = 0; i < data.Count; i++)
        {
            float height = Mathf.Clamp01(data[i] / max) * r.height;
            Rect bar = new Rect(r.x + i * barWidth, r.y + r.height - height, barWidth - 2, height);
            EditorGUI.DrawRect(bar, color);

            // Highlight spikes
            if (i > 0 && data[i] > avg * 1.5f)
            {
                EditorGUI.DrawRect(new Rect(bar.x, bar.y, bar.width, 2), Color.yellow);
            }
        }
    }

    private string FormatTime(float seconds)
    {
        if (seconds < 60)
        {
            return $"{seconds:F1} seconds";
        }

        TimeSpan span = TimeSpan.FromSeconds(seconds);
        if (span.TotalHours >= 1)
        {
            return $"{span:hh\\:mm\\:ss} hours";
        }

        return $"{span:mm\\:ss} minutes";
    }
}