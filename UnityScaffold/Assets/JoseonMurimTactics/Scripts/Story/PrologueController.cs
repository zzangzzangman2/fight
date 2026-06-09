using UnityEngine;

namespace JoseonMurimTactics
{
    /// <summary>
    /// [4] Prologue — 압록강 폐사당. 위지강의 현판령, 윤서화의 반발, 백련의 충돌,
    /// 박성준의 조선 문파 자치 선언(4성향 선택지). 종료 시 윤서화·백련 합류 후 허브로.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PrologueController : MonoBehaviour
    {
        private GameRoot root;
        private DialogueController dialogue;
        private bool leaving;

        private void Awake()
        {
            root = GameRoot.EnsureExists();
            dialogue = new DialogueController(BuildPrologue(), root);
        }

        private void OnGUI()
        {
            UiTheme.Begin(true);
            float w = Screen.width;
            float h = Screen.height;
            float s = UiTheme.Scale;

            // 장소 배너
            Rect banner = new Rect(0f, 24f * s, w, 44f * s);
            GUI.Label(banner, "제0장 · 압록강의 현판령", UiTheme.Title);
            GUI.Label(new Rect(0f, 70f * s, w, 28f * s), "의주 근처 압록강 폐사당 — 비 내리는 저녁", UiTheme.BodyCenter);

            if (!dialogue.IsFinished)
            {
                dialogue.Draw(w, h);
                return;
            }

            if (!leaving)
            {
                leaving = true;
                Finish();
            }
        }

        private void Finish()
        {
            // 동료 합류 (CH00 availableCompanionIds)
            root.Session.RecruitCompanion(CompanionCatalog.YunSeohwa);
            root.Session.RecruitCompanion(CompanionCatalog.BaekRyeon);
            root.Flags.SetFlag("FLAG_PROLOGUE_DONE");
            root.Save.Save(root.Session); // 첫 자동 저장
            root.Flow.GoToHub(SceneNames.HubPyesadang);
        }

        private static DialogueScript BuildPrologue()
        {
            DialogueScript d = new DialogueScript();

            d.Add(new DialogueNode("p0", "",
                "압록강의 물안개가 무너진 폐사당을 감싼다. 조선 소문파의 제자들이 빗속에 모여 떨고 있고, 강 건너에서 온 중원무림맹 감찰단이 횃불을 들고 도열했다.",
                "p1"));

            d.Add(new DialogueNode("p1", "감찰사 위지강",
                "“오늘부로 이 일대 조선 문파는 무림맹의 하위 분파로 편입된다. 문파명은 중원식으로 고치고, 가전 무공서를 제출하며, 모든 공식 문서는 중원 관화로만 작성하라.”",
                "p2"));

            d.Add(new DialogueNode("p2", "윤서화",
                "“…우리 검가의 현판은 백성의 피로 세운 것이오. 이름도, 무공도 당신들의 표준에 내줄 수는 없소.”",
                "p3"));

            d.Add(new DialogueNode("p3", "감찰사 위지강",
                "“변방의 계집이 말이 많구나. 표준을 거부하는 자는 질서의 적이다.” 감찰단 호위무사들이 칼자루에 손을 얹는다.",
                "p4"));

            d.Add(new DialogueNode("p4", "백련",
                "다친 제자의 상처를 싸매던 백련이 고개를 든다. “사람을 살리는 일까지 막을 셈이오? 그것이 당신들이 말하는 무림의 예요?”",
                "p5"));

            d.Add(new DialogueNode("p5", "",
                "그때, 폐사당 처마 밑에서 찢어진 문파 깃발을 주워 든 한 사내가 빗물을 털며 걸어나온다. 박성준이다. 그가 빙긋 웃으며 위지강 앞에 선다.",
                "p6"));

            DialogueNode choice = new DialogueNode("p6", "박성준",
                "(이 한마디가 조선 문파의 운명을 가를 것이다. 무엇이라 답할까?)");

            choice.choices.Add(new DialogueChoice(
                "백성의 피로 세운 문파를 남의 현판 아래 둘 수는 없소.", HeroDisposition.Chivalrous, "p7")
                .Approval(CompanionCatalog.YunSeohwa, +8)
                .Approval(CompanionCatalog.BaekRyeon, +8)
                .Faction(FactionIds.ZhongyuanAlliance, -6)
                .Faction(FactionIds.JoseonSects, +5)
                .Flag("FLAG_CHIVALROUS_STANCE")
                .Flag("FLAG_DECLARED_HAEDONG_ALLIANCE"));

            choice.choices.Add(new DialogueChoice(
                "무림의 예법을 논하려면, 먼저 사신(使臣)을 대하는 예부터 지키시오.", HeroDisposition.Royal, "p7")
                .Approval(CompanionCatalog.YunSeohwa, +3)
                .Faction(FactionIds.ZhongyuanAlliance, -4)
                .Faction(FactionIds.RoyalCourt, +4)
                .Flag("FLAG_ROYAL_STANCE")
                .Battle("dialogue_dc_down", 1));

            choice.choices.Add(new DialogueChoice(
                "내 검 아래 꿇고도 그 ‘표준’이란 말을 할 수 있는지 보자.", HeroDisposition.Conqueror, "p7")
                .Approval(CompanionCatalog.BaekRyeon, -4)
                .Approval(CompanionCatalog.YunSeohwa, +2)
                .Faction(FactionIds.ZhongyuanAlliance, -7)
                .Flag("FLAG_CONQUEROR_STANCE")
                .Battle("park_momentum", 1)
                .Battle("enemy_leader_morale", 1));

            choice.choices.Add(new DialogueChoice(
                "문파 이름도, 여인의 이름도, 억지로 바꾸라 하면 매력이 죽는 법이오.", HeroDisposition.Romantic, "p7")
                .Approval(CompanionCatalog.HanBiyeon, +8)
                .Approval(CompanionCatalog.YunSeohwa, -6)
                .Faction(FactionIds.ZhongyuanAlliance, -5)
                .Flag("FLAG_ROMANTIC_TAUNTED_INSPECTOR")
                .Battle("park_momentum", 1)
                .Battle("enemy_leader_morale", 1));

            d.Add(choice);

            d.Add(new DialogueNode("p7", "감찰사 위지강",
                "위지강의 얼굴이 일그러진다. “…좋다. 변방의 잡문파가 무림의 질서를 시험하겠다면, 그 대가를 직접 가르쳐 주마. 폐사당을 피로 씻어라!”",
                "p8"));

            d.Add(new DialogueNode("p8", "",
                "감찰단이 폐사당을 향해 진형을 좁힌다. 박성준은 찢어진 깃발을 제단에 꽂는다. 흩어져 있던 조선 문파의 첫 연합이, 이 빗속에서 시작된다.",
                null));

            return d;
        }
    }
}
