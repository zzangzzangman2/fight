using System;
using System.Collections.Generic;

namespace JoseonMurimTactics
{
[Serializable]
public sealed class GeneratedAssetManifest
{
    public int schemaVersion;
    public string sourceChecklist;
    public string scope;
    public string generatedUtc;
    public List<GeneratedAssetRecord> assets = new List<GeneratedAssetRecord>();
}

[Serializable]
public sealed class GeneratedAssetRecord
{
    public string id;
    public string category;
    public string displayName;
    public string path;
    public string addressablePath;
    public string characterId;
    public string element;
    public string weaponType;
    public string skillId;
    public string statusId;
    public string enemyId;
    public string pose;
    public string prefabPath;
    public bool isPlaceholder;
    public int width;
    public int height;
    public int transparentPixelCount;
    public int totalPixelCount;
    public List<string> aliases = new List<string>();
    public string importNotes;
}
}
