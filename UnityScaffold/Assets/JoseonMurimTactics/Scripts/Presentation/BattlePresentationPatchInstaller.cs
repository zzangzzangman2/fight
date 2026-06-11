using UnityEngine;
using UnityEngine.SceneManagement;

namespace JoseonMurimTactics
{
    /// <summary>
    /// Scene-safe runtime installer for v1.4 battle presentation patches.
    /// No scene prefab editing is required: when BattleTest loads, this adds the movement preview
    /// overlay to BattleTestController and grounding stabilizers to unit visuals.
    /// </summary>
    [DefaultExecutionOrder(9100)]
    public sealed class BattlePresentationPatchInstaller : MonoBehaviour
    {
        private float nextScanTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            InstallInCurrentScene();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            InstallInCurrentScene();
        }

        private static void InstallInCurrentScene()
        {
            BattleTestController controller = UnityEngine.Object.FindAnyObjectByType<BattleTestController>();
            if (controller == null)
            {
                return;
            }

            BattlePresentationPatchInstaller installer = controller.GetComponent<BattlePresentationPatchInstaller>();
            if (installer == null)
            {
                installer = controller.gameObject.AddComponent<BattlePresentationPatchInstaller>();
            }

            installer.InstallNow();
        }

        private void Awake()
        {
            InstallNow();
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime < nextScanTime)
            {
                return;
            }

            nextScanTime = Time.unscaledTime + 0.50f;
            InstallNow();
        }

        public void InstallNow()
        {
            BattleTestController controller = GetComponent<BattleTestController>();
            if (controller != null && controller.GetComponent<BattleClickMovementPreviewOverlay>() == null)
            {
                controller.gameObject.AddComponent<BattleClickMovementPreviewOverlay>();
            }

            CharacterVisualController[] visuals = UnityEngine.Object.FindObjectsByType<CharacterVisualController>();
            for (int i = 0; i < visuals.Length; i++)
            {
                CharacterVisualController visual = visuals[i];
                if (visual == null || visual.GetComponent<BattleUnitGroundingStabilizer>() != null)
                {
                    continue;
                }

                visual.gameObject.AddComponent<BattleUnitGroundingStabilizer>();
            }
        }
    }
}
