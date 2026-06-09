using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace JoseonMurimTactics
{
    /// <summary>
    /// 대화 진행 + 하단 대화창 렌더(IMGUI). 화자 이름/본문/초상화 placeholder/선택지를 표시하고,
    /// 선택 결과로 플래그/승인도/평판/전투보정을 GameSession에 적용한다(설계 §10).
    /// 호스트 씬이 매 OnGUI마다 Draw(...)를 호출하고 IsFinished를 확인한다.
    /// </summary>
    public sealed class DialogueController
    {
        private readonly DialogueScript script;
        private readonly GameRoot root;
        private DialogueNode current;

        public bool IsFinished { get; private set; }
        public string LastEffect { get; private set; }

        public DialogueController(DialogueScript script, GameRoot root)
        {
            this.script = script;
            this.root = root;
            current = script != null ? script.Get(script.startNodeId) : null;
            IsFinished = current == null;
        }

        public void Draw(float screenW, float screenH)
        {
            if (IsFinished || current == null)
            {
                return;
            }

            float s = UiTheme.Scale;
            float margin = 40f * s;
            bool choices = current.HasChoices;

            float boxH = (choices ? 0.40f : 0.28f) * screenH;
            Rect box = new Rect(margin, screenH - boxH - margin, screenW - margin * 2f, boxH);

            // 초상화 placeholder (대화창 위 왼쪽)
            if (!string.IsNullOrEmpty(current.speakerName))
            {
                float portrait = Mathf.Min(190f * s, screenH * 0.24f);
                Rect pr = new Rect(box.x + 6f * s, box.y - portrait - 8f * s, portrait, portrait);
                UiTheme.DrawPanel(pr, true);
                GUIStyle pName = new GUIStyle(UiTheme.BodyCenter) { fontStyle = FontStyle.Bold };
                GUI.Label(new Rect(pr.x, pr.y + pr.height * 0.5f - 18f * s, pr.width, 36f * s), current.speakerName, pName);
                GUI.Label(new Rect(pr.x, pr.yMax - 30f * s, pr.width, 26f * s), "肖像", UiTheme.SmallMuted);
            }

            UiTheme.DrawPanel(box);

            float pad = 26f * s;
            float innerX = box.x + pad;
            float innerW = box.width - pad * 2f;
            float y = box.y + 18f * s;

            // 화자 이름 플레이트
            if (!string.IsNullOrEmpty(current.speakerName))
            {
                float plateW = Mathf.Max(120f * s, current.speakerName.Length * 22f * s + 28f * s);
                UiTheme.DrawFill(new Rect(innerX, y, plateW, 38f * s), UiTheme.SealRed);
                GUIStyle nameStyle = new GUIStyle(UiTheme.Speaker) { alignment = TextAnchor.MiddleCenter };
                nameStyle.normal.textColor = UiTheme.HanjiPanel;
                GUI.Label(new Rect(innerX, y, plateW, 38f * s), current.speakerName, nameStyle);
                y += 50f * s;
            }

            // 본문
            float textH = (choices ? 84f * s : box.height - (y - box.y) - 70f * s);
            GUI.Label(new Rect(innerX, y, innerW, textH), current.line, UiTheme.Body);
            y += textH + 10f * s;

            if (choices)
            {
                float bh = 52f * s;
                float gap = 10f * s;
                for (int i = 0; i < current.choices.Count; i++)
                {
                    DialogueChoice c = current.choices[i];
                    string prefix = c.disposition.HasValue ? $"[{StoryEnumLabels.Label(c.disposition.Value)}] " : string.Empty;
                    if (GUI.Button(new Rect(innerX, y, innerW, bh), prefix + c.text, UiTheme.Button))
                    {
                        Choose(c);
                        return;
                    }

                    y += bh + gap;
                }
            }
            else
            {
                // 효과 요약(직전 선택)
                if (!string.IsNullOrEmpty(LastEffect))
                {
                    GUI.Label(new Rect(innerX, box.yMax - 60f * s, innerW * 0.7f, 28f * s), LastEffect, UiTheme.SmallMuted);
                }

                float bw = 180f * s;
                if (GUI.Button(new Rect(box.xMax - pad - bw, box.yMax - pad - 44f * s, bw, 44f * s), "계속 ▶", UiTheme.ButtonPrimary))
                {
                    Advance(current.nextNodeId);
                }
            }
        }

        private void Choose(DialogueChoice c)
        {
            LastEffect = ApplyEffects(c);
            Advance(c.nextNodeId);
        }

        private string ApplyEffects(DialogueChoice c)
        {
            List<string> parts = new List<string>();

            foreach (IdDelta d in c.approvalChanges)
            {
                if (d.delta == 0) continue;
                root.Approval.Add(d.id, d.delta);
                parts.Add($"{CompanionCatalog.Name(d.id)} 호감 {Arrow(d.delta)}");
            }

            foreach (IdDelta d in c.factionChanges)
            {
                if (d.delta == 0) continue;
                root.Reputation.Add(d.id, d.delta);
                parts.Add($"{FactionIds.Label(d.id)} {Arrow(d.delta)}");
            }

            foreach (string flag in c.flagsAdded)
            {
                root.Flags.SetFlag(flag);
            }

            foreach (IdDelta m in c.battleModifiers)
            {
                root.Flags.SetInt("battlemod:" + m.id, m.delta);
            }

            return parts.Count == 0 ? string.Empty : string.Join("   ", parts);
        }

        private static string Arrow(int delta)
        {
            return delta > 0 ? "↑" : "↓";
        }

        private void Advance(string nextId)
        {
            DialogueNode next = script.Get(nextId);
            if (next == null)
            {
                current = null;
                IsFinished = true;
                return;
            }

            current = next;
        }
    }
}
