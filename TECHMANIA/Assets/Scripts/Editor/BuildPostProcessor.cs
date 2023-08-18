using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

public class BuildPostProcessor
{
    [PostProcessBuild]
    private static void ChangeXcodePlist(
        BuildTarget buildTarget, string pathToBuiltProject)
    {
#if UNITY_IOS
        if (buildTarget == BuildTarget.iOS)
        {
            string plistPath = pathToBuiltProject + "/Info.plist";
            UnityEditor.iOS.Xcode.PlistDocument plist =
                new UnityEditor.iOS.Xcode.PlistDocument();
            plist.ReadFromFile(plistPath);
            UnityEditor.iOS.Xcode.PlistElementDict rootDict = 
                plist.root;
            rootDict.SetBoolean("UIFileSharingEnabled", true);
            rootDict.SetBoolean("UISupportsDocumentBrowser", true);
            plist.WriteToFile(plistPath);
        }
#endif
    }

    [PostProcessBuild]
    private static void CopyDefaultAssetBundle(
        BuildTarget buildTarget, string pathToBuiltProject)
    {
        string themeFolder = Path.Combine(
            Path.GetDirectoryName(pathToBuiltProject),
            Paths.kThemeFolderName);
        Directory.CreateDirectory(themeFolder);
        File.Copy(
            Path.Combine(
                Paths.kAssetBundleFolder,
                Paths.kDefaultBundleName),
            Path.Combine(
                themeFolder,
                Options.kDefaultTheme + Paths.kThemeExtension),
            overwrite: true
        );
    }

    [PostProcessBuild]
    private static void WriteVersionToFile(
        BuildTarget buildTarget, string pathToBuiltProject)
    {
        if (buildTarget == BuildTarget.StandaloneWindows ||
            buildTarget == BuildTarget.StandaloneWindows64)
        {
            File.WriteAllText(
                Path.Combine(
                    Path.GetDirectoryName(pathToBuiltProject), 
                    "version"),
                Application.version);
        }
    }
}
