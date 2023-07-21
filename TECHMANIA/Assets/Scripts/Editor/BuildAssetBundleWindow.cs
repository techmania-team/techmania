using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.Build;

public class BuildAssetBundleWindow : EditorWindow
{
    private BuildTarget buildTarget;
    private bool noCompression;

    [MenuItem("Window/TECHMANIA Theme/Build now")]
    public static void BuildForDefaultPlatform()
    {
        BuildAssetBundle(EditorUserBuildSettings.activeBuildTarget);
    }

    [MenuItem("Window/TECHMANIA Theme/Build...")]
    private static void Init()
    {
        BuildAssetBundleWindow window =
            GetWindow<BuildAssetBundleWindow>();
        window.buildTarget = EditorUserBuildSettings.activeBuildTarget;
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Target platform:");
        buildTarget = (BuildTarget)EditorGUILayout.EnumPopup(
            buildTarget);
        noCompression = EditorGUILayout.ToggleLeft("Don't compress images (overrides import settings)", noCompression);

        if (GUILayout.Button("Build"))
        {
            BuildAssetBundle(buildTarget, noCompression);
        }
    }

    private static void BuildAssetBundle(BuildTarget target,
        bool noCompression = false)
    {
        if (noCompression)
        {
            EditorUserBuildSettings.overrideTextureCompression
                = OverrideTextureCompression.ForceUncompressed;
        }
        Debug.Log($"Building theme for {target} ...");
        Directory.CreateDirectory(Paths.kAssetBundleFolder);
        AssetBundleManifest manifest =
            BuildPipeline.BuildAssetBundles(
                Paths.kAssetBundleFolder,
                BuildAssetBundleOptions.None, target);
        foreach (string bundleName in manifest.GetAllAssetBundles())
        {
            Debug.Log($"Built theme at {Paths.kAssetBundleFolder}/default.");
        }
        if (noCompression)
        {
            EditorUserBuildSettings.overrideTextureCompression
                = OverrideTextureCompression.NoOverride;
        }
    }
}
