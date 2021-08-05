using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

public class BuildPostProcessor
{
    [PostProcessBuild]
    public static void ChangeXcodePlist(
        BuildTarget buildTarget, string path)
    {
#if UNITY_IOS
        if (buildTarget == BuildTarget.iOS)
        {
            string plistPath = path + "/Info.plist";
            UnityEditor.iOS.Xcode.PlistDocument plist =
                new PlistDocument();
            plist.ReadFromFile(plistPath);
            UnityEditor.iOS.Xcode.PlistElementDict rootDict = 
                plist.root;
            rootDict.SetBoolean("UIFileSharingEnabled", true);
            rootDict.SetBoolean("UISupportsDocumentBrowser", true);
            plist.WriteToFile(plistPath);
        }
#endif
    }
}
