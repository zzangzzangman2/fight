using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
    [Serializable]
    public sealed class AuthoringContentManifest
    {
        public int version = 1;
        public string updatedAt;
        public AuthoringProject project = new AuthoringProject();
        public List<AuthoringCharacter> characters = new List<AuthoringCharacter>();
        public List<AuthoringMediaItem> backgrounds = new List<AuthoringMediaItem>();
        public List<AuthoringMediaItem> portraits = new List<AuthoringMediaItem>();
        public List<AuthoringMediaItem> props = new List<AuthoringMediaItem>();
        public List<AuthoringDialogueScene> dialogueScenes = new List<AuthoringDialogueScene>();

        public static AuthoringContentManifest LoadFromResources(string resourcePath = "AuthoringContent/content_manifest")
        {
            TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
            if (textAsset == null || string.IsNullOrEmpty(textAsset.text))
            {
                return new AuthoringContentManifest();
            }

            return JsonUtility.FromJson<AuthoringContentManifest>(textAsset.text) ?? new AuthoringContentManifest();
        }

        public AuthoringCharacter FindCharacter(string id)
        {
            return characters.Find(character => character.id == id);
        }

        public AuthoringMediaItem FindBackground(string id)
        {
            return backgrounds.Find(background => background.id == id);
        }

        public AuthoringDialogueScene FindDialogueScene(string id)
        {
            return dialogueScenes.Find(scene => scene.id == id);
        }
    }

    [Serializable]
    public sealed class AuthoringProject
    {
        public string title;
        public string note;
    }

    [Serializable]
    public sealed class AuthoringCharacter
    {
        public string id;
        public string displayName;
        public string role;
        public string portraitId;
        public string portraitResource;
        public string notes;
    }

    [Serializable]
    public sealed class AuthoringMediaItem
    {
        public string id;
        public string title;
        public string resourcePath;
        public string previewUrl;
        public string notes;
    }

    [Serializable]
    public sealed class AuthoringDialogueScene
    {
        public string id;
        public string title;
        public string location;
        public string backgroundId;
        public string startNodeId;
        public List<AuthoringDialogueEntry> entries = new List<AuthoringDialogueEntry>();
        public List<AuthoringDialogueNode> nodes = new List<AuthoringDialogueNode>();
    }

    [Serializable]
    public sealed class AuthoringDialogueEntry
    {
        public string id;
        public string speakerId;
        public string line;
        public string mood;
        public string backgroundId;
        public List<AuthoringDialogueChoice> choices = new List<AuthoringDialogueChoice>();
    }

    [Serializable]
    public sealed class AuthoringDialogueNode
    {
        public string nodeId;
        public string speakerId;
        public string speakerName;
        public string line;
        public string mood;
        public string backgroundId;
        public string portraitId;
        public string portraitResource;
        public string nextNodeId;
        public List<AuthoringDialogueChoice> choices = new List<AuthoringDialogueChoice>();
    }

    [Serializable]
    public sealed class AuthoringDialogueChoice
    {
        public string text;
        public int disposition = -1;
        public string nextNodeId;
        public List<string> requiredFlags = new List<string>();
        public List<string> flagsAdded = new List<string>();
        public List<IdDelta> approvalChanges = new List<IdDelta>();
        public List<IdDelta> factionChanges = new List<IdDelta>();
        public List<IdDelta> battleModifiers = new List<IdDelta>();
        public string sceneCommand;
    }
}
