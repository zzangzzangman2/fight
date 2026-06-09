using UnityEngine;

namespace JoseonMurimTactics
{
/// <summary>
/// Chapter 1 opening: Baekdu Cheongwang Sword Sect, Park Sungjun's first step.
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
        UiTheme.DrawMountains();

        float w = Screen.width;
        float h = Screen.height;
        float s = UiTheme.Scale;

        GUI.Label(new Rect(0f, 24f * s, w, 44f * s), "제1장 · 꺼져가는 천광", UiTheme.Title);
        GUI.Label(new Rect(0f, 70f * s, w, 28f * s), "백두산 백두천광검문 — 낡은 검각의 아침", UiTheme.BodyCenter);
        UiTheme.DrawDivider(w * 0.5f, 108f * s, 480f * s);

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
        root.Session.currentChapterId = "CHAPTER_01";
        root.Session.currentHubId = SceneNames.HubPyesadang;
        root.Flags.SetFlag(StoryFlags.PrologueCompleted);
        root.Flags.SetFlag(StoryFlags.Chapter1Started);
        root.Flags.SetFlag(StoryFlags.Chapter1TrainingIntroDone);
        root.Flags.SetFlag(StoryFlags.Chapter1VillageTrustUnlocked);
        root.Flags.SetFlag(StoryFlags.HubUnlocked);

        if (root.Flags.GetInt("silver") <= 0)
        {
            root.Flags.SetInt("silver", 30);
        }

        if (root.Flags.GetInt("supply:medicine") <= 0 || root.Inventory.GetCount("medicine_bundle") <= 0)
        {
            root.Flags.SetInt("supply:medicine", 1);
            root.Inventory.SetCount("medicine_bundle", 1);
        }

        root.Save.Save(root.Session);
        root.Flow.GoToHub();
    }

    private static DialogueScript BuildPrologue()
    {
        DialogueScript authored = TryBuildAuthoredDialogue("chapter1_prologue");
        if (authored != null)
        {
            return authored;
        }

        DialogueScript d = new DialogueScript();

        d.Add(new DialogueNode("c1_000", "",
                               "백두산 중턱, 낡은 검각. 눈은 아직 처마 끝에 걸려 있고, 찢어진 백두천광검문의 깃발은 " +
                                   "새벽바람에 힘없이 흔들린다.",
                               "c1_010"));

        d.Add(new DialogueNode("c1_010", "",
                               "예전에는 북방의 명문이라 불렸던 문파. 이제 남은 것은 병든 문주 박무겸, 엄격한 사범 " +
                                   "연옥, 그리고 오늘도 수련을 빼먹은 외동아들 박성준뿐이다.",
                               "c1_020"));

        d.Add(new DialogueNode("c1_020", "연옥", "박성준. 검각 지붕 위가 연무장이더냐?", "c1_030"));

        d.Add(new DialogueNode("c1_030", "박성준",
                               "사범님, 오해십니다. 저는 지금 고도의 심상 수련 중이었습니다. 꿈속의 저는 이미 " +
                                   "천광검문을 세 번이나 부흥시켰고요.",
                               "c1_040"));

        d.Add(new DialogueNode("c1_040", "연옥",
                               "그럼 네 꿈속의 박성준을 불러오너라. 현실의 박성준은 오늘 장작 패기와 목인 삼십 합이다.",
                               "c1_050"));

        DialogueNode excuse = new DialogueNode("c1_050", "박성준", "(어떻게 둘러댈까?)");
        excuse.choices.Add(new DialogueChoice("수련 중이었습니다. 꿈속에서.", HeroDisposition.Romantic, "c1_060")
                               .Flag("CH1_JOKED_DREAM"));
        excuse.choices.Add(new DialogueChoice("검도 쉬어야 날이 섭니다.", HeroDisposition.Royal, "c1_060")
                               .Flag("CH1_JOKED_BLADE_REST"));
        excuse.choices.Add(new DialogueChoice("사범님이 찾으실 줄 알고 기다렸죠.", HeroDisposition.Chivalrous, "c1_060")
                               .Flag("CH1_JOKED_WAITING"));
        d.Add(excuse);

        d.Add(new DialogueNode("c1_060", "연옥", "말은 늘었고, 검은 줄었구나. 내려와라. 네 아버지께서 부르신다.",
                               "c1_070"));

        d.Add(new DialogueNode("c1_070", "",
                               "박성준이 지붕에서 뛰어내리자 낡은 기와 몇 장이 와르르 미끄러진다. 연옥의 눈썹이 " +
                                   "올라가고, 성준은 아무 일 없었다는 듯 먼 산을 본다.",
                               "c1_080"));

        d.Add(new DialogueNode("c1_080", "박성준", "역시 우리 검각은 바람도 잘 통하고, 지붕도 잘 내려오는군요.",
                               "c1_090"));

        d.Add(new DialogueNode("c1_090", "연옥", "그 입이 지붕보다 먼저 무너지기 전에 가라.", "c1_100"));

        d.Add(new DialogueNode("c1_100", "박무겸",
                               "성준아. 검은 재주로 드는 것이 아니다. 짊어질 것이 있어야 드는 것이다.", "c1_110"));

        d.Add(new DialogueNode(
            "c1_110", "박성준",
            "아버지, 검은 무겁고 밥값은 더 무겁고 잔소리는 제일 무겁습니다. 셋 다 들라 하시면 아들이 좀 휘청입니다.",
            "c1_120"));

        d.Add(new DialogueNode(
            "c1_120", "박무겸",
            "네가 웃는 건 좋다. 다만 웃음 뒤에 숨지는 마라. 중원 문파들이 백두산 영맥을 노린다는 소문이 돈다.",
            "c1_130"));

        d.Add(new DialogueNode(
            "c1_130", "박무겸",
            "천광심법과 백야검결은 이제 네가 이어야 한다. 문파가 작아졌다고, 네 어깨까지 작아지는 것은 아니다.",
            "c1_140"));

        d.Add(new DialogueNode("c1_140", "박성준",
                               "제가 좀 가볍게 보여도 말이죠. 우리 문파 이름까지 가볍게 넘기진 않습니다.", "c1_150"));

        d.Add(new DialogueNode("c1_150", "",
                               "그날 낮, 성준은 소백촌으로 내려간다. 마을 사람들은 그를 아직도 사고뭉치 도련님이라 " +
                                   "부르지만, 문파를 믿는 눈빛만은 완전히 꺼지지 않았다.",
                               "c1_160"));

        d.Add(new DialogueNode("c1_160", "초희",
                               "또 수련 빼먹고 내려왔어? 약방 앞에서 폼 잡을 시간 있으면 장작이나 패.", "c1_170"));

        d.Add(new DialogueNode("c1_170", "박성준",
                               "초희야, 오늘따라 약초보다 네가 더 향기롭다? 백두산에도 봄이 오긴 오는구나.", "c1_180"));

        d.Add(new DialogueNode(
            "c1_180", "초희",
            "그 입에 붙일 약초는 없으니 그냥 가서 일이나 해. 마을도, 너희 검각도, 지금 농담만 먹고 살 수는 없어.",
            "c1_190"));

        d.Add(new DialogueNode("c1_190", "",
                               "소백촌의 일감은 작다. 장작을 패고, 약초를 캐고, 길목을 살피고, 무너진 검각을 고친다. " +
                                   "하지만 그 작은 일들이 백두천광검문을 다시 세우는 첫 돌이 된다.",
                               "c1_200"));

        DialogueNode work = new DialogueNode("c1_200", "박성준", "(오늘은 무엇부터 시작할까?)");
        work.choices.Add(
            new DialogueChoice("장작부터 패자. 은전이 있어야 밥도 먹고 지붕도 고친다.", HeroDisposition.Royal, "c1_210")
                .Faction(FactionIds.JoseonSects, +1)
                .Flag(StoryFlags.Chapter1VillageWorkStarted));
        work.choices.Add(
            new DialogueChoice("약초를 캐서 약방을 돕자. 다친 사람부터 챙겨야지.", HeroDisposition.Chivalrous, "c1_220")
                .Faction(FactionIds.JoseonSects, +2)
                .Flag(StoryFlags.Chapter1VillageWorkStarted));
        work.choices.Add(
            new DialogueChoice("검각 수리부터다. 집이 무너지면 이름도 무너진다.", HeroDisposition.Conqueror, "c1_230")
                .Faction(FactionIds.JoseonSects, +1)
                .Flag(StoryFlags.Chapter1VillageWorkStarted));
        d.Add(work);

        d.Add(new DialogueNode("c1_210", "박성준",
                               "좋아. 오늘의 백야검결 첫 초식은 장작더미 상대다. 나무야, 영광으로 알아라.", "c1_240"));

        d.Add(new DialogueNode("c1_220", "박성준",
                               "산길은 내가 좀 안다. 길 잃은 척하며 놀던 세월이 여기서 빛을 보는구만.", "c1_240"));

        d.Add(new DialogueNode("c1_230", "박성준", "검각이 집 같아야 제자도 돌아오지. 일단 비 새는 곳부터 막아보자.",
                               "c1_240"));

        d.Add(new DialogueNode(
            "c1_240", "", "그날 밤, 성준은 무너진 검각 앞에 다시 선다. 찢어진 깃발 아래로 새벽빛이 아주 얇게 스며든다.",
            "c1_250"));

        d.Add(new DialogueNode("c1_250", "박성준", "네놈들이 건드릴 건 낡은 문파가 아니야. 내 집이고, 내 사람들이다.",
                               "c1_260"));

        d.Add(new DialogueNode(
            "c1_260", "",
            "꺼져가던 천광이 아직 완전히 사라지지 않았다. 백두천광검문의 하루가, 이제 플레이어의 손에서 시작된다.",
            null));

        return d;
    }

    private static DialogueScript TryBuildAuthoredDialogue(string sceneId)
    {
        AuthoringContentManifest manifest = AuthoringContentManifest.LoadFromResources();
        DialogueScript script = AuthoringDialogueAdapter.ToDialogueScript(manifest, sceneId);
        return script.Nodes.Count > 0 ? script : null;
    }
}
}
