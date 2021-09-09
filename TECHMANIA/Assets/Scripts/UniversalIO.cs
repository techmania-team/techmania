using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Generalized IO that works on both a file system and
// streaming assets.
public static class UniversalIO
{
    public static bool FileExists(string path)
    {
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

    public static bool DirectoryExists(string path)
    {
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

    public static string ReadAllText(string path)
    {
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
}
