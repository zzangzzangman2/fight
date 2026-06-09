using System;
using System.Collections.Generic;

namespace JoseonMurimTactics
{
public sealed class DialogueRunner
{
    private readonly DialogueScript script;
    private readonly GameRoot root;
    private readonly List<string> backlog = new List<string>();

    public DialogueNode Current { get; private set; }
    public IReadOnlyList<string> Backlog => backlog;
    public bool IsFinished => Current == null;

    public event Action<DialogueNode> NodeChanged;
    public event Action Finished;

    public DialogueRunner(DialogueScript script, GameRoot root)
    {
        this.script = script;
        this.root = root;
        Current = script != null ? script.Get(script.startNodeId) : null;
        Record(Current);
    }

    public void Advance()
    {
        if (Current == null || Current.HasChoices)
        {
            return;
        }

        MoveTo(Current.nextNodeId);
    }

    public void Choose(DialogueChoice choice)
    {
        if (choice == null)
        {
            return;
        }

        ApplyEffects(choice);
        MoveTo(choice.nextNodeId);
    }

    private void MoveTo(string nodeId)
    {
        Current = script != null ? script.Get(nodeId) : null;
        if (Current == null)
        {
            Finished?.Invoke();
            return;
        }

        Record(Current);
        NodeChanged?.Invoke(Current);
    }

    private void ApplyEffects(DialogueChoice choice)
    {
        if (root == null)
        {
            return;
        }

        foreach (IdDelta delta in choice.approvalChanges)
        {
            if (choice.romanticIntent && !root.Approval.CanApplyRomanticEffect(delta.id))
            {
                continue;
            }

            root.Approval.Add(delta.id, delta.delta);
        }

        foreach (IdDelta delta in choice.factionChanges)
        {
            root.Reputation.Add(delta.id, delta.delta);
        }

        foreach (string flag in choice.flagsAdded)
        {
            root.Flags.SetFlag(flag);
        }

        foreach (IdDelta modifier in choice.battleModifiers)
        {
            root.Flags.SetInt("battlemod:" + modifier.id, modifier.delta);
        }
    }

    private void Record(DialogueNode node)
    {
        if (node == null)
        {
            return;
        }

        string speaker = string.IsNullOrEmpty(node.speakerName) ? "서술" : node.speakerName;
        backlog.Add($"{speaker}: {node.line}");
        if (backlog.Count > 80)
        {
            backlog.RemoveAt(0);
        }
    }
}
}
