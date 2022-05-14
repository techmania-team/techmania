using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class BuildAssetBundleWindow : EditorWindow
{
    private BuildTarget buildTarget;

    [MenuItem("Window/TECHMANIA/Build AssetBundles")]
    public static void BuildForDefaultPlatform()
    {
        BuildAssetBundle(EditorUserBuildSettings.activeBuildTarget);
    }

    [MenuItem("Window/TECHMANIA/Build AssetBundles (custom platform)...")]
    private static void Init()
    {
        BuildAssetBundleWindow window = 
            GetWindow<BuildAssetBundleWindow>();
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Build AssetBundles for:");
        buildTarget = (BuildTarget)EditorGUILayout.EnumPopup(
            buildTarget);
        
        if (GUILayout.Button("Build"))
        {
            BuildAssetBundle(buildTarget);
        }
    }

    private static void BuildAssetBundle(BuildTarget target)
    {
        Debug.Log($"Building asset bundle for {target} ...");
        Directory.CreateDirectory(Paths.kAssetBundleFolder);
        AssetBundleManifest manifest = 
            BuildPipeline.BuildAssetBundles(
                Paths.kAssetBundleFolder, 
                BuildAssetBundleOptions.None, target);
        foreach (string bundleName in manifest.GetAllAssetBundles())
        {
            Debug.Log($"Built AssetBundle '{bundleName}'.");
        }
    }
}
