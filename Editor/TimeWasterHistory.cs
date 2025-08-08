using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

[InitializeOnLoad]
public static class TimeWasterHistory
{
    private const string COMPILE_HISTORY_KEY = "TimeWasterTracker_CompileHistory";
    private const string DOMAIN_HISTORY_KEY = "TimeWasterTracker_DomainHistory";
    private const int MaxEntries = 50;

    public static List<float> CompileHistory { get; private set; }
    public static List<float> DomainHistory { get; private set; }

    public static string LastRecompileCause { get; set; }
    public static AdvancedCauseDetector.RecompileCause LastRecompileCauseType { get; set; }
    public static string LastDomainReloadCause { get; set; }
    public static AdvancedCauseDetector.DomainReloadCause LastDomainReloadCauseType { get; set; }
    public static List<string> RecompileCauses { get; } = new List<string>();
    public static List<string> DomainReloadCauses { get; } = new List<string>();

    static TimeWasterHistory()
    {
        CompileHistory = LoadList(COMPILE_HISTORY_KEY);
        DomainHistory = LoadList(DOMAIN_HISTORY_KEY);

        LastRecompileCause = EditorPrefs.GetString("TW_LastRecompileCause", "First initialization");
        LastRecompileCauseType = (AdvancedCauseDetector.RecompileCause)EditorPrefs.GetInt("TW_LastRecompileCauseType", (int)AdvancedCauseDetector.RecompileCause.Unknown);

        LastDomainReloadCause = EditorPrefs.GetString("TW_LastDomainReloadCause", "First initialization");
        LastDomainReloadCauseType = (AdvancedCauseDetector.DomainReloadCause)EditorPrefs.GetInt("TW_LastDomainReloadCauseType", (int)AdvancedCauseDetector.DomainReloadCause.Unknown);
    }

    public static void AddCompile(float seconds)
    {
        Add(CompileHistory, seconds);
        SaveList(COMPILE_HISTORY_KEY, CompileHistory);
    }

    public static void AddDomain(float seconds)
    {
        Add(DomainHistory, seconds);
        SaveList(DOMAIN_HISTORY_KEY, DomainHistory);
    }

    public static void AddRecompileCause(string cause, AdvancedCauseDetector.RecompileCause type)
    {
        LastRecompileCause = cause;
        LastRecompileCauseType = type;
        RecompileCauses.Add($"{DateTime.Now:HH:mm:ss} - {cause}");
        if (RecompileCauses.Count > 20) RecompileCauses.RemoveAt(0);
        SaveCauses();
    }

    public static void AddDomainReloadCause(string cause, AdvancedCauseDetector.DomainReloadCause type)
    {
        LastDomainReloadCause = cause;
        LastDomainReloadCauseType = type;
        DomainReloadCauses.Add($"{DateTime.Now:HH:mm:ss} - {cause}");
        if (DomainReloadCauses.Count > 20) DomainReloadCauses.RemoveAt(0);
        SaveCauses();
    }

    public static void Reset()
    {
        CompileHistory.Clear();
        DomainHistory.Clear();
        SaveList(COMPILE_HISTORY_KEY, CompileHistory);
        SaveList(DOMAIN_HISTORY_KEY, DomainHistory);
        LastRecompileCause = "Unknown";
        LastDomainReloadCause = "Unknown";
        SaveCauses();
    }
    private static void SaveCauses()
    {
        EditorPrefs.SetString("TW_LastRecompileCause", LastRecompileCause);
        EditorPrefs.SetInt("TW_LastRecompileCauseType", (int)LastRecompileCauseType);
        EditorPrefs.SetString("TW_LastDomainReloadCause", LastDomainReloadCause);
        EditorPrefs.SetInt("TW_LastDomainReloadCauseType", (int)LastDomainReloadCauseType);
    }

    private static void Add(List<float> list, float value)
    {
        list.Add(value);
        if (list.Count > MaxEntries)
            list.RemoveAt(0);
    }

    private static void SaveList(string key, List<float> list)
    {
        EditorPrefs.SetString(key, string.Join(",", list.Select(x => x.ToString("F2"))));
    }

    private static List<float> LoadList(string key)
    {
        string data = EditorPrefs.GetString(key, "");
        return data.Split(',')
            .Where(s => !string.IsNullOrEmpty(s) && float.TryParse(s, out _))
            .Select(float.Parse)
            .ToList();
    }
}