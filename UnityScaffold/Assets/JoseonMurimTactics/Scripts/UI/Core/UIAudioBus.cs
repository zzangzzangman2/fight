using UnityEngine;

namespace JoseonMurimTactics
{
    /// <summary>Shared UI audio entry point. Audio clips can be wired once prefab assets exist.</summary>
    [DisallowMultipleComponent]
    public sealed class UIAudioBus : MonoBehaviour
    {
        [SerializeField] private AudioSource source;
        [SerializeField] private AudioClip submitClip;
        [SerializeField] private AudioClip cancelClip;
        [SerializeField] private AudioClip hoverClip;

        private void Awake()
        {
            if (source == null)
            {
                source = GetComponent<AudioSource>();
            }
        }

        public void PlaySubmit() => Play(submitClip);
        public void PlayCancel() => Play(cancelClip);
        public void PlayHover() => Play(hoverClip);

        private void Play(AudioClip clip)
        {
            if (source != null && clip != null && GameSettings.Load().sfxVolume > 0f)
            {
                source.PlayOneShot(clip, GameSettings.Load().sfxVolume);
            }
        }
    }
}
