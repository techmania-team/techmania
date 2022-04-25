using System;
using UnityEngine;

namespace AndroidNativeIO.IO
{
    public class File
    {

        public const string ANDROID_CLASS = Constants.ANDROID_PACKAGE + ".csharp.File";
        public static AndroidJavaClass cachedClass = null;

        public static AndroidJavaClass GetCurrentClass()
        {
            if (cachedClass == null)
            {
                cachedClass = new AndroidJavaClass(ANDROID_CLASS);
            }
            return cachedClass;
        }

        public static bool Exists(string path)
        {
            return GetCurrentClass().CallStatic<bool>(
                "exists",
                Global.GetContext(),
                path
            );
        }

        public static string ReadAllText(string path)
        {
            return GetCurrentClass().CallStatic<string>(
                "readAllText",
                Global.GetContext(),
                path
            );
        }
    }
}