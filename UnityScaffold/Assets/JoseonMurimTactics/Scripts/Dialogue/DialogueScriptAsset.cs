using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
[CreateAssetMenu(menuName = "Joseon Murim/Dialogue Script")]
public sealed class DialogueScriptAsset : ScriptableObject
{
    public string scriptId;
    public string startNodeId;
    public List<DialogueNodeAsset> nodes = new List<DialogueNodeAsset>();

    public DialogueScript ToRuntimeScript()
    {
        DialogueScript script = new DialogueScript();
        foreach (DialogueNodeAsset nodeAsset in nodes)
        {
            DialogueNode node = new DialogueNode(nodeAsset.nodeId, nodeAsset.speakerName, nodeAsset.line,
                                                 nodeAsset.nextNodeId, nodeAsset.speakerId);
            node.portraitResource = nodeAsset.portraitResource;
            node.backgroundId = nodeAsset.backgroundId;
            node.backgroundResource = nodeAsset.backgroundResource;

            foreach (DialogueChoiceAsset choiceAsset in nodeAsset.choices)
            {
                DialogueChoice choice =
                    new DialogueChoice(choiceAsset.text, choiceAsset.disposition, choiceAsset.nextNodeId);
                choice.romanticIntent = choiceAsset.romanticIntent;
                foreach (IdDelta delta in choiceAsset.approvalDeltas)
                    choice.approvalChanges.Add(delta);
                foreach (IdDelta delta in choiceAsset.reputationDeltas)
                    choice.factionChanges.Add(delta);
                foreach (string flag in choiceAsset.setFlags)
                    choice.flagsAdded.Add(flag);
                foreach (IdDelta modifier in choiceAsset.battleModifiers)
                    choice.battleModifiers.Add(modifier);
                node.choices.Add(choice);
            }

            script.Add(node);
        }

        if (!string.IsNullOrEmpty(startNodeId))
        {
            script.startNodeId = startNodeId;
        }

        return script;
    }
}

[Serializable]
public sealed class DialogueNodeAsset
{
    public string nodeId;
    public string speakerId;
    public string speakerName;
    public string portraitResource;
    public string backgroundId;
    public string backgroundResource;
    [TextArea(2, 5)]
    public string line;
    public string nextNodeId;
    public List<DialogueChoiceAsset> choices = new List<DialogueChoiceAsset>();
}

[Serializable]
public sealed class DialogueChoiceAsset
{
    public string text;
    public HeroDisposition disposition;
    public string nextNodeId;
    public List<string> requiredFlags = new List<string>();
    public List<string> setFlags = new List<string>();
    public List<IdDelta> approvalDeltas = new List<IdDelta>();
    public List<IdDelta> reputationDeltas = new List<IdDelta>();
    public List<IdDelta> battleModifiers = new List<IdDelta>();
    public bool romanticIntent;
    public string sceneCommand;
}
}
