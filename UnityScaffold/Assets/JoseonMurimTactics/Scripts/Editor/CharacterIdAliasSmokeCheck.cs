using UnityEditor;
using UnityEngine;

namespace JoseonMurimTactics.Editor
{
public static class CharacterIdAliasSmokeCheck
{
    public static void Run()
    {
        bool ok = true;
        void Check(bool condition, string message)
        {
            if (condition)
            {
                return;
            }

            ok = false;
            Debug.LogError("[CharacterIdAliasSmokeCheck] " + message);
        }

        Check(CharacterGrowthCatalog.NormalizeCharacterId("seo_a") == CharacterIdAliasResolver.ShinSeoaId,
              "seo_a should normalize to shin_seoa.");
        Check(CompanionCatalog.Info("seo_a") != null &&
              CompanionCatalog.Info("seo_a").id == CharacterIdAliasResolver.ShinSeoaId,
              "CompanionCatalog should resolve seo_a as shin_seoa.");
        Check(GiftCatalog.Get("gift_flower_ribbon").IsFavoriteOf("seo_a"),
              "Shin Seoa favorite gift should still work with seo_a alias.");

        GameSession session = new GameSession();
        session.RecruitCompanion("seo_a");
        session.companionApproval["seo_a"] = 72;
        session.intVars["growth:seo_a:level"] = 3;
        session.intVars["gift:last_day:seo_a"] = 2;
        session.stringVars["equip:seo_a:weapon"] = "test_fan";

        GameSession loaded = GameSession.FromJson(session.ToJson());
        Check(loaded.HasCompanion("shin_seoa"), "Recruited seo_a should load as shin_seoa.");
        Check(!loaded.recruitedCompanionIds.Contains("seo_a"), "Loaded roster should not keep seo_a.");
        Check(loaded.companionApproval.TryGetValue("shin_seoa", out int approval) && approval == 72,
              "Approval should migrate from seo_a to shin_seoa.");
        Check(loaded.intVars.ContainsKey("growth:shin_seoa:level"), "Growth keys should migrate to shin_seoa.");
        Check(loaded.intVars.ContainsKey("gift:last_day:shin_seoa"), "Gift day keys should migrate to shin_seoa.");
        Check(loaded.stringVars.ContainsKey("equip:shin_seoa:weapon"), "Equipment keys should migrate to shin_seoa.");

        AuthoringContentManifest manifest = AuthoringContentManifest.LoadFromResources();
        Check(manifest.FindCharacter("seo_a") != null &&
              manifest.FindCharacter("seo_a").id == CharacterIdAliasResolver.ShinSeoaId,
              "Authoring manifest should find shin_seoa through seo_a alias.");
        Check(manifest.FindDialogueScene("companion_shin_seoa_talk") != null,
              "Authoring manifest should contain companion_shin_seoa_talk.");

        if (!ok)
        {
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("[CharacterIdAliasSmokeCheck] Character ID alias and save migration smoke check passed.");
        EditorApplication.Exit(0);
    }
}
}
