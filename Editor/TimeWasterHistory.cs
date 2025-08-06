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

    static TimeWasterHistory()
    {
        CompileHistory = LoadList(COMPILE_HISTORY_KEY);
        DomainHistory = LoadList(DOMAIN_HISTORY_KEY);
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

    public static void Reset()
    {
        CompileHistory.Clear();
        DomainHistory.Clear();
        SaveList(COMPILE_HISTORY_KEY, CompileHistory);
        SaveList(DOMAIN_HISTORY_KEY, DomainHistory);
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