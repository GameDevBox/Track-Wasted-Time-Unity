using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using static AdvancedCauseDetector;
using System;

[InitializeOnLoad]
public static class AdvancedCauseDetector
{
    private static Dictionary<string, long> lastScriptTimestamps;
    private static List<string> recentlyChangedScripts = new List<string>();
    private static DateTime lastCheckTime;
    private static bool wasPlaying = false;

    // Common causes we can detect
    public enum RecompileCause
    {
        ScriptModification,
        AssetImport,
        AssemblyDefinitionChange,
        PlayModeEnter,
        PlayModeExit,
        ScriptCompilationError,
        EditorPrefsChange,
        ManualRefresh,
        Unknown
    }

    public enum DomainReloadCause
    {
        PlayModeEnter,
        PlayModeExit,
        ScriptModification,
        AssemblyReload,
        EditorWindowFocus,
        Unknown
    }

    static AdvancedCauseDetector()
    {
        lastScriptTimestamps = GetScriptTimestamps();
        lastCheckTime = DateTime.Now;
        wasPlaying = EditorApplication.isPlaying;

        EditorApplication.update += OnEditorUpdate;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.projectChanged += OnProjectChanged;
        EditorApplication.delayCall += InitializeAssetTracking;
    }

    private static void InitializeAssetTracking()
    {
        // Set up asset postprocessor for tracking asset changes
    }

    private static void OnEditorUpdate()
    {
        // Check every second for changes
        if ((DateTime.Now - lastCheckTime).TotalSeconds > 1)
        {
            DetectScriptChanges();
            CheckPlayModeTransition();
            lastCheckTime = DateTime.Now;
        }
    }

    private static void DetectScriptChanges()
    {
        var currentTimestamps = GetScriptTimestamps();
        recentlyChangedScripts.Clear();

        foreach (var pair in currentTimestamps)
        {
            if (!lastScriptTimestamps.ContainsKey(pair.Key) ||
                lastScriptTimestamps[pair.Key] != pair.Value)
            {
                recentlyChangedScripts.Add(Path.GetFileName(pair.Key));
            }
        }

        if (recentlyChangedScripts.Count > 0)
        {
            string changedFiles = string.Join(", ", recentlyChangedScripts.Take(3));
            if (recentlyChangedScripts.Count > 3) changedFiles += ", ...";

            TimeWasterHistory.LastRecompileCause =
                $"Script changes detected in: {changedFiles}";
            TimeWasterHistory.LastRecompileCauseType = RecompileCause.ScriptModification;
        }

        lastScriptTimestamps = currentTimestamps;
    }

    private static Dictionary<string, long> GetScriptTimestamps()
    {
        string[] scriptPaths = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);
        return scriptPaths.ToDictionary(
            path => path,
            path => File.GetLastWriteTimeUtc(path).Ticks
        );
    }

    private static void CheckPlayModeTransition()
    {
        bool isPlaying = EditorApplication.isPlaying;
        if (isPlaying != wasPlaying)
        {
            if (isPlaying)
            {
                TimeWasterHistory.LastDomainReloadCause = "Entering Play Mode";
                TimeWasterHistory.LastDomainReloadCauseType = DomainReloadCause.PlayModeEnter;
            }
            else
            {
                TimeWasterHistory.LastDomainReloadCause = "Exiting Play Mode";
                TimeWasterHistory.LastDomainReloadCauseType = DomainReloadCause.PlayModeExit;
            }
            wasPlaying = isPlaying;
        }
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        switch (state)
        {
            case PlayModeStateChange.ExitingEditMode:
                TimeWasterHistory.LastDomainReloadCause = "Preparing to enter Play Mode";
                break;
            case PlayModeStateChange.EnteredPlayMode:
                TimeWasterHistory.LastDomainReloadCause = "Finished entering Play Mode";
                break;
            case PlayModeStateChange.ExitingPlayMode:
                TimeWasterHistory.LastDomainReloadCause = "Preparing to exit Play Mode";
                break;
            case PlayModeStateChange.EnteredEditMode:
                TimeWasterHistory.LastDomainReloadCause = "Finished exiting Play Mode";
                break;
        }
    }

    private static void OnProjectChanged()
    {
        // This fires when assets are imported or project structure changes
        if (!EditorApplication.isCompiling)
        {
            TimeWasterHistory.LastRecompileCause = "Project assets were modified";
        }
    }
}

// Enhanced Asset Import Detector
public class AdvancedAssetTracker : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        if (importedAssets.Length > 0)
        {
            string assetTypes = string.Join(", ",
                importedAssets.Select(a => Path.GetExtension(a)).Distinct());

            TimeWasterHistory.LastRecompileCause =
                $"Assets imported ({assetTypes})";
            TimeWasterHistory.LastRecompileCauseType = RecompileCause.AssetImport;

            // Detect if any asmdef files were changed
            if (importedAssets.Any(a => a.EndsWith(".asmdef")))
            {
                TimeWasterHistory.LastRecompileCause = "Assembly definition modified";
                TimeWasterHistory.LastRecompileCauseType = RecompileCause.AssemblyDefinitionChange;
            }
        }
    }
}