using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace JoseonMurimTactics.Editor
{
public static class Living2DVisualAssetMigrator
{
    private const string CharacterRoot = "Assets/JoseonMurimTactics/Art/Characters";
    private const string ClipRoot = CharacterRoot + "/Common/AnimationClips2D";
    private const string ProfileRoot = CharacterRoot + "/Common/LivingMotionProfiles";

    [MenuItem("Tools/Joseon Murim/Characters/Generate Missing Living 2D Clips")]
    public static void GenerateMissingLiving2DClips()
    {
        GenerateMissingLiving2DClipsBatch();
    }

    public static void GenerateMissingLiving2DClipsBatch()
    {
        AssetDatabase.Refresh();
        EnsureFolder(ClipRoot);
        EnsureFolder(ProfileRoot);

        int changed = 0;
        string[] guids = AssetDatabase.FindAssets("t:CharacterVisualData", new[] { CharacterRoot });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!IsPlayableCharacterVisual(path))
            {
                continue;
            }

            CharacterVisualData visual = AssetDatabase.LoadAssetAtPath<CharacterVisualData>(path);
            if (visual == null)
            {
                continue;
            }

            string id = string.IsNullOrWhiteSpace(visual.visualId) ? InferCharacterId(path) : visual.visualId;
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            EnsureFolder(ClipRoot + "/" + id);
            if (visual.livingMotion == null)
            {
                visual.livingMotion = CreateOrLoadProfile(id);
                changed++;
            }

            changed += AssignMissingClips(id, visual);
            BackfillCueIfEmpty(visual.moveClip, "move");
            BackfillCueIfEmpty(visual.attackClip, "attack");
            BackfillCueIfEmpty(visual.skillClip, "skill");
            BackfillCueIfEmpty(visual.hitClip, "hit");
            BackfillCueIfEmpty(visual.guardClip, "guard");
            BackfillCueIfEmpty(visual.turnStartClip, "turn_start");
            BackfillCueIfEmpty(visual.lowHpClip, "low_hp");

            EditorUtility.SetDirty(visual);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Living2DVisualAssetMigrator] Living 2D migration complete. Changed slots: " + changed);
    }

    private static int AssignMissingClips(string id, CharacterVisualData visual)
    {
        int changed = 0;
        if (visual.idleClip == null)
        {
            visual.idleClip = CreateOrLoadClip(id, "idle", Frames(visual.idleFrames, visual.idlePoseSprite),
                                               6f, true, true, true, Vector2.zero, 0f, null);
            changed++;
        }

        if (visual.selectedIdleClip == null)
        {
            visual.selectedIdleClip = CreateOrLoadClip(id, "selected_idle", Frames(visual.idleFrames, visual.idlePoseSprite),
                                                       6f, true, true, true, Vector2.zero, 0f, null);
            changed++;
        }

        if (visual.moveClip == null)
        {
            visual.moveClip = CreateOrLoadClip(id, "move", Frames(visual.moveFrames, visual.movePoseSprite),
                                               12f, true, false, true, Vector2.zero, 0f, MoveCues());
            changed++;
        }

        if (visual.attackClip == null)
        {
            visual.attackClip = CreateOrLoadClip(id, "attack", Frames(visual.attackFrames, visual.attackPoseSprite),
                                                 14f, false, false, false, new Vector2(0.018f, 0f), 0f, AttackCues());
            changed++;
        }

        if (visual.skillClip == null)
        {
            visual.skillClip = CreateOrLoadClip(id, "skill", Frames(visual.skillFrames, visual.skillPoseSprite),
                                                12f, false, false, false, new Vector2(0.012f, 0.012f), 0f, SkillCues());
            changed++;
        }

        if (visual.hitClip == null)
        {
            visual.hitClip = CreateOrLoadClip(id, "hit", Frames(visual.hitFrames, visual.hitPoseSprite),
                                              12f, false, false, false, new Vector2(-0.010f, 0f), 0f, HitCues());
            changed++;
        }

        if (visual.guardClip == null)
        {
            visual.guardClip = CreateOrLoadClip(id, "guard", Frames(null, visual.attackPoseSprite != null ? visual.attackPoseSprite : visual.idlePoseSprite),
                                                8f, false, false, true, Vector2.zero, 0.34f, GuardCues());
            changed++;
        }

        if (visual.waitClip == null)
        {
            visual.waitClip = CreateOrLoadClip(id, "wait", Frames(null, visual.actedPoseSprite != null ? visual.actedPoseSprite : visual.idlePoseSprite),
                                               4f, true, true, true, Vector2.zero, 0f, null);
            changed++;
        }

        if (visual.defeatClip == null)
        {
            visual.defeatClip = CreateOrLoadClip(id, "defeat", Frames(null, visual.defeatedPoseSprite != null ? visual.defeatedPoseSprite : visual.hitPoseSprite),
                                                 6f, false, false, true, new Vector2(-0.020f, -0.016f), 0.58f, null);
            changed++;
        }

        if (visual.victoryClip == null)
        {
            visual.victoryClip = CreateOrLoadClip(id, "victory", Frames(null, visual.skillPoseSprite != null ? visual.skillPoseSprite : visual.idlePoseSprite),
                                                  7f, true, true, true, new Vector2(0f, 0.006f), 0f, null);
            changed++;
        }

        if (visual.turnStartClip == null)
        {
            visual.turnStartClip = CreateOrLoadClip(id, "turn_start", Frames(new[] { visual.idlePoseSprite, visual.attackPoseSprite, visual.idlePoseSprite }, visual.idlePoseSprite),
                                                    10f, false, false, true, new Vector2(0f, 0.010f), 0.32f, TurnStartCues());
            changed++;
        }

        if (visual.lowHpClip == null)
        {
            visual.lowHpClip = CreateOrLoadClip(id, "low_hp", Frames(visual.idleFrames, visual.idlePoseSprite),
                                                5f, true, true, true, new Vector2(-0.004f, -0.004f), 0f, LowHpCues());
            changed++;
        }

        visual.enableBlink = true;
        visual.enableLayerSway = true;
        visual.enableFootDust = false;
        visual.enableSelectionPop = true;
        visual.enableImpactFreeze = true;
        return changed;
    }

    private static CharacterLivingMotionProfile CreateOrLoadProfile(string id)
    {
        string path = ProfileRoot + "/living_profile_" + id + ".asset";
        CharacterLivingMotionProfile profile = LoadOrCreate<CharacterLivingMotionProfile>(path);
        ApplyProfileDefaults(id, profile);
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static void ApplyProfileDefaults(string id, CharacterLivingMotionProfile profile)
    {
        profile.idleBreathSpeed = 1f;
        profile.idleBobAmount = 0.030f;
        profile.idleBobSpeed = 1f;
        profile.blinkMinInterval = 3f;
        profile.blinkMaxInterval = 6.5f;
        profile.selectedPopDuration = 0.14f;
        profile.turnStartDuration = 0.32f;
        profile.waitSlouchAmount = 0.36f;
        profile.lowHpShakeAmount = 0.010f;
        profile.stateEnterPopScale = 1.045f;
        profile.stateEnterLift = 0.034f;
        profile.stateEnterDuration = 0.13f;
        profile.moveLeanAmount = 0.82f;
        profile.footDustAmount = 0f;
        profile.attackAnticipationDistance = 0.050f;
        profile.hitShakeAmount = 0.026f;
        profile.victoryHopAmount = 0.060f;
        profile.motionTrailAlpha = 0.16f;
        profile.motionTrailDuration = 0.15f;
        profile.accessorySwayAmount = 0.78f;

        switch (id)
        {
        case "do_arin":
            SetMotion(profile, 0.018f, 1.075f, 0.100f, 0.050f, 1.12f, 0.058f, 1.25f, 1.25f, 1.00f);
            break;
        case "jin_seoyul":
            SetMotion(profile, 0.011f, 1.050f, 0.065f, 0.032f, 1.00f, 0.064f, 0.90f, 1.15f, 0.90f);
            break;
        case "shin_seoa":
            SetMotion(profile, 0.016f, 1.070f, 0.095f, 0.052f, 0.92f, 0.048f, 1.45f, 1.00f, 1.40f);
            break;
        case "han_biyeon":
            SetMotion(profile, 0.010f, 1.040f, 0.055f, 0.026f, 1.18f, 0.055f, 0.95f, 1.35f, 0.78f);
            break;
        default:
            SetMotion(profile, 0.014f, 1.055f, 0.075f, 0.035f, 1.05f, 0.060f, 0.90f, 1.10f, 0.78f);
            break;
        }
    }

    private static void SetMotion(CharacterLivingMotionProfile profile, float breath, float selectedPop,
                                  float turnHop, float moveHop, float lunge, float freeze,
                                  float hair, float weapon, float accessory)
    {
        profile.idleBreathAmount = breath;
        profile.selectedPopScale = selectedPop;
        profile.turnStartHop = turnHop;
        profile.moveHopAmount = moveHop;
        profile.attackLungeMultiplier = lunge;
        profile.impactFreezeSeconds = freeze;
        profile.hairSwayAmount = hair;
        profile.weaponSwayAmount = weapon;
        profile.accessorySwayAmount = accessory;
    }

    private static CharacterSpriteAnimationClipData CreateOrLoadClip(string id, string state, Sprite[] frames,
                                                                      float fps, bool loop, bool pingPong,
                                                                      bool holdLast, Vector2 rootOffset,
                                                                      float durationOverride,
                                                                      CharacterSpriteAnimationEventCue[] cues)
    {
        string path = ClipRoot + "/" + id + "/clip_" + id + "_" + state + ".asset";
        CharacterSpriteAnimationClipData clip = LoadOrCreate<CharacterSpriteAnimationClipData>(path);
        clip.clipId = id + "_" + state;
        clip.frames = frames;
        clip.framesPerSecond = fps;
        clip.loop = loop;
        clip.pingPong = pingPong;
        clip.holdLastFrame = holdLast;
        clip.rootOffset = rootOffset;
        clip.durationOverride = durationOverride;
        clip.eventCues = cues ?? clip.eventCues ?? new CharacterSpriteAnimationEventCue[0];
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void BackfillCueIfEmpty(CharacterSpriteAnimationClipData clip, string state)
    {
        if (clip == null || clip.eventCues != null && clip.eventCues.Length > 0)
        {
            return;
        }

        CharacterSpriteAnimationEventCue[] cues = CuesForState(state);
        if (cues == null)
        {
            return;
        }

        clip.eventCues = cues;
        EditorUtility.SetDirty(clip);
    }

    private static CharacterSpriteAnimationEventCue[] CuesForState(string state)
    {
        switch (state)
        {
        case "move":
            return MoveCues();
        case "attack":
            return AttackCues();
        case "skill":
            return SkillCues();
        case "hit":
            return HitCues();
        case "guard":
            return GuardCues();
        case "turn_start":
            return TurnStartCues();
        case "low_hp":
            return LowHpCues();
        default:
            return null;
        }
    }

    private static CharacterSpriteAnimationEventCue[] MoveCues()
    {
        return new[] { Cue("footstep_left", 0.18f), Cue("footstep_right", 0.68f) };
    }

    private static CharacterSpriteAnimationEventCue[] AttackCues()
    {
        return new[] { Cue("anticipation", 0.18f), Cue("impact", 0.52f), Cue("recover", 0.78f) };
    }

    private static CharacterSpriteAnimationEventCue[] SkillCues()
    {
        return new[] { Cue("cast", 0.20f), Cue("afterglow", 0.42f), Cue("impact", 0.62f) };
    }

    private static CharacterSpriteAnimationEventCue[] HitCues()
    {
        return new[] { Cue("hit_flash", 0.10f) };
    }

    private static CharacterSpriteAnimationEventCue[] GuardCues()
    {
        return new[] { Cue("guard", 0.20f) };
    }

    private static CharacterSpriteAnimationEventCue[] TurnStartCues()
    {
        return new[] { Cue("ready", 0.18f) };
    }

    private static CharacterSpriteAnimationEventCue[] LowHpCues()
    {
        return new[] { Cue("weak", 0.50f) };
    }

    private static CharacterSpriteAnimationEventCue Cue(string cueId, float normalizedTime)
    {
        return new CharacterSpriteAnimationEventCue { cueId = cueId, normalizedTime = normalizedTime };
    }

    private static Sprite[] Frames(Sprite[] frames, Sprite fallback)
    {
        if (frames != null && frames.Length > 0)
        {
            List<Sprite> valid = new List<Sprite>();
            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] != null)
                {
                    valid.Add(frames[i]);
                }
            }

            if (valid.Count > 0)
            {
                return valid.ToArray();
            }
        }

        return fallback == null ? new Sprite[0] : new[] { fallback };
    }

    private static bool IsPlayableCharacterVisual(string path)
    {
        return path.Contains("/VisualData/") &&
               !path.Contains("/Enemies/") &&
               !path.Contains("/TestSwordsman/") &&
               !path.Contains("/Common/");
    }

    private static string InferCharacterId(string path)
    {
        string[] parts = path.Split('/');
        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (parts[i] == "Characters")
            {
                return parts[i + 1];
            }
        }

        return null;
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
        {
            return asset;
        }

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void EnsureFolder(string folder)
    {
        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }
}
}
