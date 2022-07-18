using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Generalized IO that works on PC, Android and iOS.
// Also supports streaming assets on iOS.
public static class UniversalIO
{
    // Paths returned by the Android file picker will be in this scheme.
    // We can call GetRealPathFromUri to translate it to a file system path and do regular I/O, but there are limitations:
    // * Android 12 doesn't allow direct reading of these files, unless it's a media file
    // * Enumerating directories won't show non-media files at all
    // Therefore, unless reading a media file, all I/O should go through methods in this class.
    public const string ANDROID_CONTENT_URI = "content://";

    public static bool IsAndroidContentURI(string path)
    {
        return path.StartsWith(ANDROID_CONTENT_URI);
    }

    // This method will decode the content uri back to linux file path, but the limitations still apply.
    // Only use this for display on the UI or reading media file.
    public static string GetAndroidRealPath(string path)
    {
        if (IsAndroidContentURI(path))
        {
            return AndroidNativeIO.Utils.StorageUtils.GetRealPathFromUri(path);
        }
        return path;
    }

    #region Directory
    public static class Directory
    {
        public static void CreateDirectory(string parentPath, string childFolderName)
        {
            if (IsAndroidContentURI(parentPath))
            {
                if (!AndroidNativeIO.IO.Directory.CreateDirectory(parentPath, childFolderName))
                {
                    throw new IOException("Failed to create directory (Android Native IO)");
                }
            }
            else
            {
                System.IO.Directory.CreateDirectory(Path.Combine(parentPath, childFolderName));
            }
        }


        public static IEnumerable<string> EnumerateFiles(string path, string searchPattern)
        {
            if (IsAndroidContentURI(path))
            {
                return AndroidNativeIO.IO.Directory.EnumerateFiles(path, searchPattern);
            }
            return System.IO.Directory.EnumerateFiles(path, searchPattern);
        }

        public static IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption)
        {
            if (IsAndroidContentURI(path))
            {
                return AndroidNativeIO.IO.Directory.EnumerateFiles(path, searchPattern);
            }
            return System.IO.Directory.EnumerateFiles(path, searchPattern, searchOption);
        }

        public static IEnumerable<string> EnumerateDirectories(string path)
        {
            if (IsAndroidContentURI(path))
            {
                return AndroidNativeIO.IO.Directory.EnumerateDirectories(path);
            }
            return System.IO.Directory.EnumerateDirectories(path);
        }

        public static string GetName(string path)
        {
            if (IsAndroidContentURI(path))
            {
                return AndroidNativeIO.IO.Directory.GetName(path);
            }
            return new DirectoryInfo(path).Name;
        }

        public static bool Exists(string path)
        {
            if (IsAndroidContentURI(path))
            {
                return AndroidNativeIO.IO.Directory.Exists(path);
            }
            if (Paths.IsInStreamingAssets(path))
            {
                return BetterStreamingAssets.DirectoryExists(
                    Paths.RelativePathInStreamingAssets(path));
            }
            else
            {
                return System.IO.Directory.Exists(path);
            }
        }
    }
    #endregion


    #region File
    public static class File
    {
        public static bool Exists(string path)
        {
            if (IsAndroidContentURI(path))
            {
                return AndroidNativeIO.IO.File.Exists(path);
            }
            if (Paths.IsInStreamingAssets(path))
            {
                return BetterStreamingAssets.FileExists(
                    Paths.RelativePathInStreamingAssets(path));
            }
            else
            {
                return System.IO.File.Exists(path);
            }
        }

        public static string ReadAllText(string path)
        {
            if (IsAndroidContentURI(path))
            {
                return AndroidNativeIO.IO.File.ReadAllText(path);
            }
            if (Paths.IsInStreamingAssets(path))
            {
                return BetterStreamingAssets.ReadAllText(
                    Paths.RelativePathInStreamingAssets(path));
            }
            else
            {
                return System.IO.File.ReadAllText(path);
            }
        }

        public static void WriteAllText(string path, string content)
        {
            if (IsAndroidContentURI(path))
            {
                AndroidNativeIO.IO.File.WriteAllText(path, content);
            }
            else
            {
                System.IO.File.WriteAllText(path, content);
            }
        }
        public static FileStream Create(string path)
        {
            return System.IO.File.Create(path);
        }

        public static void Delete(string path)
        {
            System.IO.File.Delete(path);
        }

        public static FileStream OpenRead(string path)
        {
            return System.IO.File.OpenRead(path);
        }
    }
    #endregion

    #region Path
    public static class Path
    {
        public static string GetDirectoryName(string path, bool returnToTreeUri = true)
        {

            if (IsAndroidContentURI(path))
            {
                return AndroidNativeIO.IO.Path.GetDirectoryName(path, returnToTreeUri);
            }
            return System.IO.Path.GetDirectoryName(path);
        }

        public static string Combine(string parent, string name)
        {
            if (IsAndroidContentURI(parent))
            {
                return AndroidNativeIO.IO.Path.Combine(parent, name);
            }
            return System.IO.Path.Combine(parent, name);
        }
    }

    #endregion
}
