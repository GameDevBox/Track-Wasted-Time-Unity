using UnityEditor;
using UnityEngine;
using System.IO;
using System;

[InitializeOnLoad]
public static class RecompileDetector
{
    private static string[] lastScriptHashes;
    private static DateTime lastCheckTime;

    static RecompileDetector()
    {
        lastScriptHashes = GetScriptHashes();
        lastCheckTime = DateTime.Now;

        EditorApplication.update += OnEditorUpdate;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnEditorUpdate()
    {
        // Check every 2 seconds for script changes
        if ((DateTime.Now - lastCheckTime).TotalSeconds > 2)
        {
            CheckForScriptChanges();
            lastCheckTime = DateTime.Now;
        }
    }

    private static void CheckForScriptChanges()
    {
        string[] currentHashes = GetScriptHashes();
        bool hasChanges = false;

        for (int i = 0; i < currentHashes.Length; i++)
        {
            if (i >= lastScriptHashes.Length || currentHashes[i] != lastScriptHashes[i])
            {
                hasChanges = true;
                break;
            }
        }

        if (hasChanges)
        {
            Debug.LogWarning("🔄 Script modifications detected (likely recompile trigger)");
            TimeWasterHistory.LastRecompileCause = "Script modifications";
        }

        lastScriptHashes = currentHashes;
    }

    private static string[] GetScriptHashes()
    {
        string[] scriptPaths = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);
        string[] hashes = new string[scriptPaths.Length];

        for (int i = 0; i < scriptPaths.Length; i++)
        {
            hashes[i] = File.GetLastWriteTime(scriptPaths[i]).Ticks.ToString();
        }

        return hashes;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            TimeWasterHistory.LastDomainReloadCause = "Entering Play Mode";
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            TimeWasterHistory.LastDomainReloadCause = "Exiting Play Mode";
        }
    }
}