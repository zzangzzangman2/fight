using System.Collections.Generic;

namespace JoseonMurimTactics
{
    public static class AuthoringDialogueAdapter
    {
        public static DialogueScript ToDialogueScript(AuthoringContentManifest manifest, string sceneId)
        {
            if (manifest == null)
            {
                return new DialogueScript();
            }

            return ToDialogueScript(manifest, manifest.FindDialogueScene(sceneId));
        }

        public static DialogueScript ToDialogueScript(AuthoringContentManifest manifest, AuthoringDialogueScene scene)
        {
            DialogueScript script = new DialogueScript();
            if (scene == null)
            {
                return script;
            }

            IReadOnlyList<AuthoringDialogueNode> nodes = scene.nodes;
            foreach (AuthoringDialogueNode source in nodes)
            {
                DialogueNode node = new DialogueNode(
                    source.nodeId,
                    ResolveSpeakerName(manifest, source),
                    source.line,
                    source.nextNodeId,
                    source.speakerId);

                foreach (AuthoringDialogueChoice choiceSource in source.choices)
                {
                    DialogueChoice choice = new DialogueChoice(
                        choiceSource.text,
                        ToDisposition(choiceSource.disposition),
                        choiceSource.nextNodeId);

                    foreach (IdDelta delta in choiceSource.approvalChanges) choice.approvalChanges.Add(delta);
                    foreach (IdDelta delta in choiceSource.factionChanges) choice.factionChanges.Add(delta);
                    foreach (string flag in choiceSource.flagsAdded) choice.flagsAdded.Add(flag);
                    foreach (IdDelta modifier in choiceSource.battleModifiers) choice.battleModifiers.Add(modifier);
                    node.choices.Add(choice);
                }

                script.Add(node);
            }

            if (!string.IsNullOrEmpty(scene.startNodeId))
            {
                script.startNodeId = scene.startNodeId;
            }

            return script;
        }

        private static string ResolveSpeakerName(AuthoringContentManifest manifest, AuthoringDialogueNode node)
        {
            if (!string.IsNullOrEmpty(node.speakerName))
            {
                return node.speakerName;
            }

            AuthoringCharacter character = manifest != null ? manifest.FindCharacter(node.speakerId) : null;
            return character == null ? string.Empty : character.displayName;
        }

        private static HeroDisposition? ToDisposition(int value)
        {
            if (value < 0 || value > 3)
            {
                return null;
            }

            return (HeroDisposition)value;
        }
    }
}
