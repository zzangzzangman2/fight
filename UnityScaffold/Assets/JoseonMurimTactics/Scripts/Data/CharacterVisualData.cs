using UnityEngine;

namespace JoseonMurimTactics
{
    [CreateAssetMenu(menuName = "Joseon Murim Tactics/Character Visual Data")]
    public sealed class CharacterVisualData : ScriptableObject
    {
        public string visualId;
        public Sprite fullBodySprite;
        public Sprite bustSprite;
        public Sprite portraitSprite;
        public RuntimeAnimatorController animatorController;

        [Header("Board Fit")]
        public float heightInTiles = 1.18f;
        public Vector2 spriteOffset = new Vector2(0f, 0.12f);
        public int sortingOffset;

        [Header("Presence")]
        public float idleAmplitude = 0.035f;
        public float idleSpeed = 1f;
        public float breathingScale = 0.015f;
        public float shadowWidth = 0.72f;
        public float shadowHeight = 0.18f;

        [Header("State Colors")]
        public Color normalTint = Color.white;
        public Color selectedTint = new Color(1f, 0.92f, 0.62f, 1f);
        public Color defeatedTint = new Color(0.55f, 0.55f, 0.55f, 0.68f);
    }
}
