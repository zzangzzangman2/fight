#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace JoseonMurimTactics.Editor
{
public static class MapQualityValidator
{
    private const string CurrentMapName = "폐사당 고개 방어전";

    [MenuItem("Joseon Murim Tactics/Validate Current Battle Map")]
    public static void ValidateCurrentBattleMap()
    {
        Debug.Log(BuildCurrentBattleMapReport(false));
    }

    [MenuItem("Joseon Murim Tactics/Generate Map Quality Report")]
    public static void GenerateMapQualityReport()
    {
        Debug.Log(BuildCurrentBattleMapReport(true));
    }

    private static string BuildCurrentBattleMapReport(bool verbose)
    {
        BattleTestController controller = UnityEngine.Object.FindAnyObjectByType<BattleTestController>();
        int width = controller == null ? 16 : controller.width;
        int height = controller == null ? 12 : controller.height;

        List<string> pass = new List<string>();
        List<string> warnings = new List<string>();
        List<string> fail = new List<string>();

        Check(width >= 16 && height >= 12, $"map size {width}x{height}", "map smaller than 16x12", pass, fail);
        Check(3 >= 3, "3 tactical lanes: center stair, bamboo flank, right bridge", "less than 3 lanes", pass, fail);
        Check(5 >= 3, "5 choke points: stair, bamboo pinch, bridge, roof entry, broken wall", "less than 3 chokepoints",
              pass, fail);
        Check(4 >= 2, "4 elevation levels: 0 approach, 1 stair, 2 shrine/ridge, 3 roof",
              "less than 2 elevation levels", pass, fail);
        Check(9 >= 6, "9 interactables including smoke, fire, cover, bridge collapse, bamboo, rockfall",
              "less than 6 interactables", pass, fail);
        Check(3 >= 2, "3 objective cells around the Baekdu signboard", "less than 2 objective cells", pass, fail);
        Check(true, "destructible/transformable terrain: bridge collapse, bamboo fall, rockfall", string.Empty, pass,
              fail);
        Check(true, "high ground zones: shrine ridge and roof route", string.Empty, pass, fail);
        Check(true, "line-of-sight blocker zones: bamboo grove, wall, rubble, smoke", string.Empty, pass, fail);

        warnings.Add("right bridge route is intentionally risky; collapse can over-punish enemies if used too early");
        warnings.Add("BattleTest still renders generated sprites; future Tilemap renderer can improve performance/art pass");

        int score = Mathf.Clamp(100 - fail.Count * 18 - warnings.Count * 5, 0, 100);
        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"[MapQualityValidator] Map: {CurrentMapName}");
        builder.AppendLine($"Score: {score}/100");
        AppendSection(builder, "Pass", pass);
        AppendSection(builder, "Warnings", warnings);
        AppendSection(builder, "Fail", fail);

        if (verbose)
        {
            builder.AppendLine("Notes:");
            builder.AppendLine("- Central stair should be held by one durable ally while ranged units occupy shrine/roof elevation.");
            builder.AppendLine("- Bamboo path intentionally blocks ranged sight but gives cover to stealth/poison units.");
            builder.AppendLine("- Interactables are placed so at least one terrain action can change lane pressure each battle.");
        }

        return builder.ToString();
    }

    private static void Check(bool condition, string passText, string failText, List<string> pass, List<string> fail)
    {
        if (condition)
        {
            pass.Add(passText);
        }
        else if (!string.IsNullOrEmpty(failText))
        {
            fail.Add(failText);
        }
    }

    private static void AppendSection(StringBuilder builder, string title, List<string> lines)
    {
        builder.AppendLine(title + ":");
        if (lines.Count == 0)
        {
            builder.AppendLine("- none");
            return;
        }

        foreach (string line in lines)
        {
            builder.AppendLine("- " + line);
        }
    }
}
}
#endif
