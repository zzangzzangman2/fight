using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
public sealed class CombatLog : MonoBehaviour
{
    private readonly List<string> entries = new List<string>();

    public IReadOnlyList<string> Entries
    {
        get {
            return entries;
        }
    }

    public void Add(string category, string message)
    {
        string entry = "[" + category + "] " + message;
        entries.Add(entry);
        Debug.Log(entry);
    }

    public void Clear()
    {
        entries.Clear();
    }
}
}
