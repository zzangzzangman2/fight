using System.Collections;
using UnityEngine;

namespace JoseonMurimTactics
{
    /// <summary>Canvas-friendly fade state for the future UI root. GameRoot keeps the current scene fade.</summary>
    [DisallowMultipleComponent]
    public sealed class ScreenFadeController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;

        public bool IsFading { get; private set; }

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        public Coroutine FadeTo(float alpha, float duration)
        {
            StopAllCoroutines();
            return StartCoroutine(FadeRoutine(alpha, Mathf.Max(0.01f, duration)));
        }

        private IEnumerator FadeRoutine(float target, float duration)
        {
            if (canvasGroup == null)
            {
                yield break;
            }

            IsFading = true;
            float start = canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            canvasGroup.alpha = target;
            IsFading = false;
        }
    }
}
