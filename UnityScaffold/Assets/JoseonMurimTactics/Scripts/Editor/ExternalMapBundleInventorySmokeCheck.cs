using System.IO;
using UnityEditor;
using UnityEngine;

namespace JoseonMurimTactics.Editor
{
public static class ExternalMapBundleInventorySmokeCheck
{
    public static void Run()
    {
        string folder = ExternalMapBundleBrowser.AbsoluteBundleFolder;
        bool ok = true;
        if (!Directory.Exists(folder))
        {
            Debug.LogError("[ExternalMapBundleInventorySmokeCheck] MAP folder missing: " + folder);
            ok = false;
        }
        else
        {
            int count = ExternalMapBundleBrowser.BundleCount;
            if (count != 456)
            {
                Debug.LogError(
                    "[ExternalMapBundleInventorySmokeCheck] Expected 456 map bundles, found " + count
                );
                ok = false;
            }
            else
            {
                Debug.Log(
                    "[ExternalMapBundleInventorySmokeCheck] PASS MAP bundle count=456 folder=" + folder
                );
                string samplePath = Path.Combine(folder, "stage_plain-cp-ni00201_snow.unity3d");
                if (!File.Exists(samplePath))
                {
                    Debug.LogError(
                        "[ExternalMapBundleInventorySmokeCheck] Sample snow map bundle missing: " + samplePath
                    );
                    ok = false;
                }
                else
                {
                    AssetBundle bundle = AssetBundle.LoadFromFile(samplePath);
                    if (bundle == null)
                    {
                        Debug.LogError(
                            "[ExternalMapBundleInventorySmokeCheck] Unity could not load sample bundle: " +
                            samplePath
                        );
                        ok = false;
                    }
                    else
                    {
                        if (bundle.isStreamedSceneAssetBundle)
                        {
                            string[] scenePaths = bundle.GetAllScenePaths();
                            Debug.Log(
                                "[ExternalMapBundleInventorySmokeCheck] PASS sample scene bundle scenes=" +
                                scenePaths.Length
                            );
                        }
                        else
                        {
                            Texture2D[] textures = bundle.LoadAllAssets<Texture2D>();
                            GameObject[] gameObjects = bundle.LoadAllAssets<GameObject>();
                            Debug.Log(
                                "[ExternalMapBundleInventorySmokeCheck] PASS sample load textures=" +
                                textures.Length + " gameObjects=" + gameObjects.Length
                            );
                        }
                        bundle.Unload(true);
                    }
                }
            }
        }

        EditorApplication.Exit(ok ? 0 : 1);
    }
}
}
