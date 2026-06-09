using UnityEngine;

namespace JoseonMurimTactics
{
    /// <summary>Canvas screen base for the v1.0 UI pass. Current IMGUI screens can coexist while prefabs are built.</summary>
    [DisallowMultipleComponent]
    public abstract class UIScreenBase : MonoBehaviour
    {
        [SerializeField] private string screenId;
        [SerializeField] private CanvasGroup canvasGroup;

        public string ScreenId => string.IsNullOrEmpty(screenId) ? name : screenId;
        public bool IsVisible { get; private set; }

        protected virtual void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        public virtual void Show()
        {
            IsVisible = true;
            SetCanvasState(true);
            OnShown();
        }

        public virtual void Hide()
        {
            IsVisible = false;
            SetCanvasState(false);
            OnHidden();
        }

        protected virtual void OnShown() { }
        protected virtual void OnHidden() { }

        private void SetCanvasState(bool visible)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }

            gameObject.SetActive(visible);
        }
    }
}
