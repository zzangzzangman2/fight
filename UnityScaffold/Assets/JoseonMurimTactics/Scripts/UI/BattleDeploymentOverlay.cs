using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JoseonMurimTactics
{
    /// <summary>
    /// BattlePrepController를 크게 뜯지 않고 붙이는 IMGUI 출진 선택 오버레이.
    /// 선택값은 BattleDeploymentService가 GameSession.storyFlags에 저장하므로 기존 출격 버튼을 눌러도 그대로 적용된다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BattleDeploymentOverlay : MonoBehaviour
    {
        private const string ObjectName = "BattleDeploymentOverlay_v1_3";
        private GameRoot root;
        private BattleDefinition definition;
        private readonly List<string> candidates = new List<string>();
        private readonly HashSet<string> selectedCompanions = new HashSet<string>();
        private string boundBattleId;
        private bool collapsed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallHooks()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            TryCreateForScene(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryCreateForScene(scene);
        }

        private static void TryCreateForScene(Scene scene)
        {
            if (scene.name != "BattlePrep")
            {
                return;
            }

            if (GameObject.Find(ObjectName) != null)
            {
                return;
            }

            GameObject go = new GameObject(ObjectName);
            go.AddComponent<BattleDeploymentOverlay>();
        }

        private void Awake()
        {
            root = GameRoot.EnsureExists();
            RefreshBinding(true);
        }

        private void Update()
        {
            RefreshBinding(false);
        }

        private void RefreshBinding(bool force)
        {
            string battleId = string.IsNullOrEmpty(BattleEntryAdapter.PendingBattleId) ? HubController.FirstBattleId : BattleEntryAdapter.PendingBattleId;
            if (!force && battleId == boundBattleId)
            {
                return;
            }

            boundBattleId = battleId;
            definition = root != null && root.BattleRepository != null ? root.BattleRepository.Get(battleId) : BattleCatalog.Get(battleId);
            candidates.Clear();
            candidates.AddRange(BattleDeploymentService.GetCandidateCompanions(root.Session, definition));

            selectedCompanions.Clear();
            BattleDeploymentService.EnsureDefaultStored(root.Session, definition);
            List<string> active = BattleDeploymentService.GetActiveParty(root.Session);
            if (active.Count <= 0)
            {
                active = BattleDeploymentService.BuildDefaultParty(root.Session, definition);
            }

            for (int i = 0; i < active.Count; i++)
            {
                string id = active[i];
                if (id != CharacterGrowthCatalog.ProtagonistId)
                {
                    selectedCompanions.Add(id);
                }
            }

            SaveSelection();
        }

        private void OnGUI()
        {
            if (SceneManager.GetActiveScene().name != "BattlePrep")
            {
                return;
            }

            UiTheme.Begin(false);
            GUI.depth = -260;
            float s = UiTheme.Scale;
            float width = 326f * s;
            float headerHeight = collapsed ? 58f * s : 92f * s;
            float rowHeight = 36f * s;
            float height = collapsed ? headerHeight : headerHeight + Mathf.Max(1, candidates.Count) * rowHeight + 94f * s;
            Rect panel = new Rect(Screen.width - width - 38f * s, 118f * s, width, height);
            UiTheme.DrawPanel(panel, true);

            float x = panel.x + 18f * s;
            float y = panel.y + 14f * s;
            float innerW = panel.width - 36f * s;

            GUI.Label(new Rect(x, y, innerW - 84f * s, 28f * s), "출진 파티", UiTheme.Heading);
            if (GUI.Button(new Rect(panel.xMax - 88f * s, y - 2f * s, 68f * s, 30f * s), collapsed ? "펼침" : "접기", UiTheme.Button))
            {
                collapsed = !collapsed;
            }

            y += 32f * s;
            if (collapsed)
            {
                GUI.Label(new Rect(x, y, innerW, 22f * s), "동료 " + selectedCompanions.Count + "/" + BattleDeploymentService.MaxCompanionSlots + "명 선택됨", UiTheme.SmallMuted);
                return;
            }

            GUI.Label(new Rect(x, y, innerW, 22f * s), "박성준 고정 + 선택 동료만 성장", UiTheme.SmallMuted);
            y += 30f * s;

            Rect heroRect = new Rect(x, y, innerW, 30f * s);
            UiTheme.DrawFill(heroRect, new Color(UiTheme.NavyLight.r, UiTheme.NavyLight.g, UiTheme.NavyLight.b, 0.46f));
            GUI.Label(new Rect(heroRect.x + 10f * s, heroRect.y + 5f * s, heroRect.width - 20f * s, 24f * s), "✓ 박성준  — 주인공 고정 출진", UiTheme.Small);
            y += rowHeight;

            ProgressionService progression = new ProgressionService(root.Session);
            for (int i = 0; i < candidates.Count; i++)
            {
                string id = candidates[i];
                bool selected = selectedCompanions.Contains(id);
                bool canAdd = selected || selectedCompanions.Count < BattleDeploymentService.MaxCompanionSlots;
                CharacterProgressState state = progression.GetSnapshot(id);
                string label = (selected ? "✓ " : "＋ ") + CharacterGrowthCatalog.DisplayName(id) + "  Lv." + state.level + " " + state.realmName;

                GUI.enabled = canAdd;
                GUIStyle style = selected ? UiTheme.ButtonPrimary : UiTheme.Button;
                if (GUI.Button(new Rect(x, y, innerW, 31f * s), label, style))
                {
                    if (selected)
                    {
                        selectedCompanions.Remove(id);
                    }
                    else if (selectedCompanions.Count < BattleDeploymentService.MaxCompanionSlots)
                    {
                        selectedCompanions.Add(id);
                    }

                    SaveSelection();
                }
                GUI.enabled = true;
                y += rowHeight;
            }

            y += 8f * s;
            GUI.Label(new Rect(x, y, innerW, 24f * s), "선택 " + selectedCompanions.Count + "/" + BattleDeploymentService.MaxCompanionSlots + " · 비출진 동료 XP/숙련/유대 없음", UiTheme.SmallMuted);
            y += 26f * s;

            if (GUI.Button(new Rect(x, y, innerW, 34f * s), "기본 편성으로 되돌리기", UiTheme.Button))
            {
                ResetDefault();
            }
        }

        private void SaveSelection()
        {
            if (root == null || root.Session == null)
            {
                return;
            }

            BattleDeploymentService.SetActiveParty(root.Session, definition, selectedCompanions);
        }

        private void ResetDefault()
        {
            selectedCompanions.Clear();
            List<string> party = BattleDeploymentService.BuildDefaultParty(root.Session, definition);
            for (int i = 0; i < party.Count; i++)
            {
                if (party[i] != CharacterGrowthCatalog.ProtagonistId)
                {
                    selectedCompanions.Add(party[i]);
                }
            }

            SaveSelection();
        }
    }
}
