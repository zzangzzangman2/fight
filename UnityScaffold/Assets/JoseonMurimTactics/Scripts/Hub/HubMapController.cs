using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
[DisallowMultipleComponent]
public sealed class HubMapController : MonoBehaviour
{
    [SerializeField]
    private HubLocationPanel locationPanel;
    [SerializeField]
    private List<HubHotspot> hotspots = new List<HubHotspot>();
    [SerializeField]
    private HubLocation currentLocation = HubLocation.Courtyard;

    private GameRoot root;

    private void Awake()
    {
        root = GameRoot.EnsureExists();
        if (hotspots.Count == 0)
        {
            hotspots.AddRange(GetComponentsInChildren<HubHotspot>(true));
        }

        RefreshHotspots();
        Select(currentLocation);
    }

    public void Select(HubLocation location)
    {
        currentLocation = location;
        if (locationPanel != null)
        {
            locationPanel.Bind(root, currentLocation);
        }
    }

    public void RefreshHotspots()
    {
        foreach (HubHotspot hotspot in hotspots)
        {
            if (hotspot == null)
            {
                continue;
            }

            hotspot.Bind(this, hotspot.Location, HubLocationPanel.Label(hotspot.Location), BadgeFor(hotspot.Location));
        }
    }

    private string BadgeFor(HubLocation location)
    {
        if (root == null)
        {
            return string.Empty;
        }

        switch (location)
        {
        case HubLocation.MissionGate:
            return root.Flags.HasFlag(StoryFlags.FirstBattleWon) ? "新" : "!";
        case HubLocation.CompanionDeck:
            return root.Session.recruitedCompanionIds.Count > 0 ? "話" : string.Empty;
        case HubLocation.Infirmary:
            return HasWounded() ? "傷" : string.Empty;
        default:
            return string.Empty;
        }
    }

    private bool HasWounded()
    {
        BattleResultData last = root != null ? root.Session.lastBattleResult : null;
        return last != null && last.woundedCompanions != null && last.woundedCompanions.Count > 0;
    }
}
}
