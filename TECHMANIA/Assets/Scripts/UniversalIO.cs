using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Generalized IO that works on both a file system and
// streaming assets.
public static class UniversalIO
{
    public const string ANDROID_CONTENT_URI = "content://";

    public static string GetRealPathFromUri(string path)
    {

        if (path.StartsWith(ANDROID_CONTENT_URI))
        {
            return AndroidNativeIO.Utils.StorageUtils.GetRealPathFromUri(path);
        }
        return path;
    }

    #region Directory
    public static DirectoryInfo DirectoryCreateDirectory(string path)
    {
        return Directory.CreateDirectory(path);
    }

    public static IEnumerable<string> DirectoryEnumerateFiles(string path, string searchPattern)
    {
        if (path.StartsWith(ANDROID_CONTENT_URI))
        {
            return AndroidNativeIO.IO.Directory.EnumerateFiles(path, searchPattern);
        }
        return Directory.EnumerateFiles(path, searchPattern);
    }

    public static IEnumerable<string> DirectoryEnumerateFiles(string path, string searchPattern, SearchOption searchOption)
    {
        if (path.StartsWith(ANDROID_CONTENT_URI))
        {
            return AndroidNativeIO.IO.Directory.EnumerateFiles(path, searchPattern);
        }
        return Directory.EnumerateFiles(path, searchPattern, searchOption);
    }

    public static IEnumerable<string> DirectoryEnumerateDirectories(string path)
    {
        if (path.StartsWith(ANDROID_CONTENT_URI))
        {
            return AndroidNativeIO.IO.Directory.EnumerateDirectories(path);
        }
        return Directory.EnumerateDirectories(path);
    }

    public static string DirectoryGetCurrentDirectory()
    {
        return Directory.GetCurrentDirectory();
    }

    public static string DirectoryGetName(string path)
    {
        return new DirectoryInfo(path).Name;
    }

    public static bool DirectoryExists(string path)
    {
        if (path.StartsWith(ANDROID_CONTENT_URI))
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
            return Directory.Exists(path);
        }
    }
    #endregion


    #region File
    public static FileStream FileCreate(string path)
    {
        return File.Create(path);
    }

    public static void FileDelete(string path)
    {
        File.Delete(path);
    }

    public static bool FileExists(string path)
    {
        if (path.StartsWith(ANDROID_CONTENT_URI))
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
            return File.Exists(path);
        }
    }

    public static FileStream FileOpenRead(string path)
    {
        return File.OpenRead(path);
    }

    public static string ReadAllText(string path)
    {
        if (path.StartsWith(ANDROID_CONTENT_URI))
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
            return File.ReadAllText(path);
        }
    }

    #endregion

    #region Path

    public static string PathGetDirectoryName(string path)
    {
        return Path.GetDirectoryName(path);
    }

    public static string PathCombine(params string[] path)
    {
        if (path[0].StartsWith(ANDROID_CONTENT_URI))
        {
            return AndroidNativeIO.IO.Path.Combine(path[0], path[1]);
        }
        return Path.Combine(path);
    }

    #endregion
}
