
using UnityEngine;
using AndroidNativeIO;

namespace AndroidNativeIO.Utils
{
    public class StorageUtils
    {

        public const string ANDROID_CLASS = Constants.ANDROID_PACKAGE + ".AndroidStorage";

        public static AndroidJavaClass cachedClass = null;

        public static AndroidJavaClass GetCurrentClass()
        {
            if (cachedClass == null)
            {
                cachedClass = new AndroidJavaClass(ANDROID_CLASS);
            }
            return cachedClass;
        }

        public static void OpenStorageFolder(string callbackGameObject,
            string resultCallbackMethod, string errorCallbackMethod)
        {
            Global.GetContext().Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                GetCurrentClass().CallStatic(
                    "openDocumentTree",
                    Global.GetContext(),
                    callbackGameObject,
                    resultCallbackMethod,
                    errorCallbackMethod
                );
            }));
        }

        public static string GetRealPathFromUri(string path)
        {
            return GetCurrentClass().CallStatic<string>(
                "getRealPathFromContentUri",
                Global.GetContext(),
                path
            );
        }
    }
}