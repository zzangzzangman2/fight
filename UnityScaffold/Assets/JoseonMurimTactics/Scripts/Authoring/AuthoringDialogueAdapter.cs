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
            AuthoringCharacter character = manifest != null ? manifest.FindCharacter(source.speakerId) : null;
            DialogueNode node = new DialogueNode(source.nodeId, ResolveSpeakerName(character, source), source.line,
                                                 source.nextNodeId, source.speakerId);
            node.speakerTitle = ResolveSpeakerTitle(character);
            string portraitResource = PortraitRegistry.ResolvePortraitResource(
                source.speakerId,
                !string.IsNullOrEmpty(source.portraitId) ? source.portraitId : character != null ? character.portraitId : null,
                !string.IsNullOrEmpty(source.portraitResource)
                    ? source.portraitResource
                    : character != null ? character.portraitResource : null);
            node.portraitResource = PortraitRegistry.ResolveMoodPortraitResource(source.speakerId, source.mood, portraitResource);
            node.backgroundId = DialogueBackgroundRegistry.ResolveBackgroundId(source.backgroundId, scene.backgroundId);
            AuthoringMediaItem background = manifest != null ? manifest.FindBackground(node.backgroundId) : null;
            node.backgroundResource = background != null && !string.IsNullOrEmpty(background.resourcePath)
                                          ? background.resourcePath
                                          : DialogueBackgroundRegistry.ResolveResourcePath(node.backgroundId, scene.backgroundId);

            foreach (AuthoringDialogueChoice choiceSource in source.choices)
            {
                DialogueChoice choice = new DialogueChoice(choiceSource.text, ToDisposition(choiceSource.disposition),
                                                           choiceSource.nextNodeId);
                choice.romanticIntent = choiceSource.romanticIntent;

                foreach (IdDelta delta in choiceSource.approvalChanges)
                    choice.approvalChanges.Add(delta);
                foreach (IdDelta delta in choiceSource.factionChanges)
                    choice.factionChanges.Add(delta);
                foreach (string flag in choiceSource.flagsAdded)
                    choice.flagsAdded.Add(flag);
                foreach (IdDelta modifier in choiceSource.battleModifiers)
                    choice.battleModifiers.Add(modifier);
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

    private static string ResolveSpeakerName(AuthoringCharacter character, AuthoringDialogueNode node)
    {
        if (!string.IsNullOrEmpty(node.speakerName))
        {
            return node.speakerName;
        }

        return character == null ? string.Empty : character.displayName;
    }

    private static string ResolveSpeakerTitle(AuthoringCharacter character)
    {
        if (character == null)
        {
            return null;
        }

        return !string.IsNullOrEmpty(character.sectName) ? character.sectName : character.role;
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
