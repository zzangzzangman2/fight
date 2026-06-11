using System;
using UnityEngine;

namespace JoseonMurimTactics
{
[Serializable]
public struct CharacterSpriteAnimationEventCue
{
    public string cueId;
    [Range(0f, 1f)] public float normalizedTime;
    public string payload;
}

[CreateAssetMenu(menuName = "Joseon Murim Tactics/Character Sprite Animation Clip Data")]
public sealed class CharacterSpriteAnimationClipData : ScriptableObject
{
    public string clipId;
    public Sprite[] frames;
    public float framesPerSecond = 8f;
    public bool loop = true;
    public bool pingPong;
    public bool holdLastFrame = true;
    public Vector2 rootOffset;
    public float durationOverride;
    public CharacterSpriteAnimationEventCue[] eventCues;

    public bool HasFrames => frames != null && frames.Length > 0;

    public float Duration
    {
        get
        {
            if (durationOverride > 0f)
            {
                return durationOverride;
            }

            if (!HasFrames)
            {
                return 0f;
            }

            return Mathf.Max(0.01f, frames.Length / Mathf.Max(0.01f, framesPerSecond));
        }
    }

    public Sprite Evaluate(float elapsedSeconds, float normalizedFallback, bool defaultLoop)
    {
        if (!HasFrames)
        {
            return null;
        }

        if (frames.Length == 1)
        {
            return frames[0];
        }

        bool shouldLoop = loop || defaultLoop;
        float t;
        if (Duration > 0f)
        {
            t = shouldLoop ? Mathf.Repeat(elapsedSeconds / Duration, 1f)
                           : Mathf.Clamp01(elapsedSeconds / Duration);
        }
        else
        {
            t = shouldLoop ? Mathf.Repeat(normalizedFallback, 1f) : Mathf.Clamp01(normalizedFallback);
        }

        if (pingPong)
        {
            t = Mathf.PingPong(t * 2f, 1f);
        }

        int index;
        if (!shouldLoop && holdLastFrame && t >= 1f)
        {
            index = frames.Length - 1;
        }
        else
        {
            index = Mathf.Min(frames.Length - 1, Mathf.FloorToInt(t * frames.Length));
        }

        return frames[Mathf.Clamp(index, 0, frames.Length - 1)];
    }
}
}
