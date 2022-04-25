
using System;
using UnityEngine;

namespace AndroidNativeIO.IO
{
    public class Directory
    {

        public const string ANDROID_CLASS = Constants.ANDROID_PACKAGE + ".csharp.Directory";
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

        public static string[] EnumerateDirectories(string path)
        {
            using (
                var obj = GetCurrentClass().CallStatic<AndroidJavaObject>(
                    "enumerateDirectories",
                    Global.GetContext(),
                    path
                )
            )
            {

                if (obj.GetRawObject().ToInt32() != 0)
                {
                    String[] result = AndroidJNIHelper.ConvertFromJNIArray<String[]>(obj.GetRawObject());
                    return result;
                }
                return new string[] { };
            }

        }

        public static string[] EnumerateFiles(string path, string searchPattern)
        {
            using (
                var obj = GetCurrentClass().CallStatic<AndroidJavaObject>(
                    "enumerateFiles",
                    Global.GetContext(),
                    path,
                    searchPattern
                )
            )
            {

                if (obj.GetRawObject().ToInt32() != 0)
                {
                    String[] result = AndroidJNIHelper.ConvertFromJNIArray<String[]>(obj.GetRawObject());
                    return result;
                }
                return new string[] { };
            }

        }
    }
}