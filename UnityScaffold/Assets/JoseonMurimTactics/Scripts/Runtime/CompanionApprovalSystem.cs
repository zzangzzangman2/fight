using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
public sealed class CompanionApprovalSystem : MonoBehaviour
{
    private readonly Dictionary<string, int> approval = new Dictionary<string, int>();

    public int GetApproval(string companionId)
    {
        return approval.ContainsKey(companionId) ? approval[companionId] : 50;
    }

    public void AddApproval(string companionId, int delta)
    {
        int current = GetApproval(companionId);
        approval[companionId] = Mathf.Clamp(current + delta, 0, 100);
    }

    public void ResolveParkCharmRisk(bool failedPsychologicalMove)
    {
        if (!failedPsychologicalMove)
        {
            return;
        }

        List<string> keys = new List<string>(approval.Keys);
        foreach (string key in keys)
        {
            AddApproval(key, -3);
        }
    }
}
}
