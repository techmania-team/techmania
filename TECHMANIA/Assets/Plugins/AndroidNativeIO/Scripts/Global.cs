using UnityEngine;
using System;


namespace AndroidNativeIO
{
    // This points to a compiled library at Assets/Plugins/AndroidNativeIO/lib-release.aar
    // Source code: https://github.com/samnyan/UnityNativeAndroidIO

    public class Constants
    {
        public const string ANDROID_PACKAGE = "cn.samnya.nativeandroid.io";
    }

    public class Global
    {

        private static AndroidJavaClass cachedUnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        private static AndroidJavaObject cachedUnityPlayerContext = null;

        public static AndroidJavaObject InitializeContext()
        {
            if (cachedUnityPlayer == null)
            {
                cachedUnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            }
            AndroidJavaObject context = cachedUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            cachedUnityPlayerContext = context;
            return context;
        }

        public static AndroidJavaObject GetContext()
        {
            if (cachedUnityPlayerContext != null)
            {
                return cachedUnityPlayerContext;
            }
            else
            {
                return InitializeContext();
            }
        }

    }
}