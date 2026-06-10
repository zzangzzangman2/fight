using System;
using System.Collections.Generic;

namespace JoseonMurimTactics
{
/// <summary>id(동료/세력) → 변화량.</summary>
[Serializable]
public struct IdDelta
{
    public string id;
    public int delta;

    public IdDelta(string id, int delta)
    {
        this.id = id;
        this.delta = delta;
    }
}

/// <summary>대화 선택지. 대사뿐 아니라 승인도/평판/플래그/전투 보정에 영향을 준다(설계 §10).</summary>
public sealed class DialogueChoice
{
    public string text;
    public HeroDisposition? disposition; // 선택지 성향 아이콘
    public List<IdDelta> approvalChanges = new List<IdDelta>();
    public List<IdDelta> factionChanges = new List<IdDelta>();
    public List<string> flagsAdded = new List<string>();
    public List<IdDelta> battleModifiers = new List<IdDelta>(); // 전투 시작 보정(키→값)
    public string nextNodeId;                                   // null/empty면 대화 종료
    public bool romanticIntent;                                 // romance/flirt 전용 안전 태그

    public DialogueChoice(string text, HeroDisposition? disposition = null, string nextNodeId = null)
    {
        this.text = text;
        this.disposition = disposition;
        this.nextNodeId = nextNodeId;
    }

    public DialogueChoice Approval(string companionId, int delta)
    {
        approvalChanges.Add(new IdDelta(companionId, delta));
        return this;
    }

    public DialogueChoice Faction(string factionId, int delta)
    {
        factionChanges.Add(new IdDelta(factionId, delta));
        return this;
    }

    public DialogueChoice Flag(string flag)
    {
        flagsAdded.Add(flag);
        return this;
    }

    public DialogueChoice Battle(string key, int value)
    {
        battleModifiers.Add(new IdDelta(key, value));
        return this;
    }

    public DialogueChoice RomanticIntent()
    {
        romanticIntent = true;
        return this;
    }
}

/// <summary>대화 노드. 한 화면 분량의 대사 + (선택지 또는 다음 노드).</summary>
public sealed class DialogueNode
{
    public string id;
    public string speakerId;
    public string speakerName;
    public string speakerTitle;     // 이름 옆 소속/직함 태그(예: 백두천광검문 소문주). 비면 표시 생략.
    public string portraitResource; // Resources 경로의 전신/스탠딩 일러. 비면 표시 생략.
    public string line;
    public List<DialogueChoice> choices = new List<DialogueChoice>();
    public string nextNodeId; // 선택지가 없을 때 다음 노드. null이면 종료.

    public DialogueNode(string id, string speakerName, string line, string nextNodeId = null, string speakerId = null)
    {
        this.id = id;
        this.speakerName = speakerName;
        this.line = line;
        this.nextNodeId = nextNodeId;
        this.speakerId = speakerId;
    }

    public bool HasChoices => choices != null && choices.Count > 0;
}

/// <summary>대화 한 편.</summary>
public sealed class DialogueScript
{
    public string startNodeId;
    private readonly Dictionary<string, DialogueNode> nodes = new Dictionary<string, DialogueNode>();
    private readonly List<DialogueNode> order = new List<DialogueNode>();

    public DialogueNode Add(DialogueNode node)
    {
        if (node == null || string.IsNullOrEmpty(node.id))
        {
            return node;
        }

        if (string.IsNullOrEmpty(startNodeId))
        {
            startNodeId = node.id;
        }

        nodes[node.id] = node;
        order.Add(node);
        return node;
    }

    public DialogueNode Get(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        return nodes.TryGetValue(id, out DialogueNode node) ? node : null;
    }

    public IReadOnlyList<DialogueNode> Nodes => order;
}
}
