using System;
using UnityEngine;

namespace AndroidNativeIO.IO
{
    public class Path
    {

        public const string ANDROID_CLASS = Constants.ANDROID_PACKAGE + ".csharp.Path";
        public static AndroidJavaClass cachedClass = null;

        public static AndroidJavaClass GetCurrentClass()
        {
            if (cachedClass == null)
            {
                cachedClass = new AndroidJavaClass(ANDROID_CLASS);
            }
            return cachedClass;
        }

        public static string Combine(string path1, string path2)
        {
            return GetCurrentClass().CallStatic<string>(
                "combine",
                Global.GetContext(),
                path1,
                path2
            );
        }
    }
}