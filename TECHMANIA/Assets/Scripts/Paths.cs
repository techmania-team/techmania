using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using UnityEngine.Events;

public static class Paths
{
    public static string GetTrackFolder()
    {
        string current = Directory.GetCurrentDirectory();  // Does not end with \
        // If there's a "Builds" folder, assume we are running from
        // Unity editor.
        if (Directory.Exists(current + "\\Builds"))
        {
            current += "\\Builds";
        }

        string tracks = current + "\\Tracks";
        if (!Directory.Exists(tracks))
        {
            Directory.CreateDirectory(tracks);
        }
        return tracks;
    }

    private static string GetDataFolder()
    {
        string folder = System.Environment.GetFolderPath(
            System.Environment.SpecialFolder.MyDocuments)
            + "\\TECHMANIA";
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }
        return folder;
    }

    public static string GetOptionsFilePath()
    {
        return GetDataFolder() + "\\options.json";
    }

    private static List<string> GetAllMatchingFiles(string folder, List<string> patterns)
    {
        List<string> files = new List<string>();
        foreach (string pattern in patterns)
        {
            foreach (string file in Directory.EnumerateFiles(folder, pattern))
            {
                files.Add(file);
            }
        }
        return files;
    }

    public static List<string> GetAllAudioFiles(string folder)
    {
        return GetAllMatchingFiles(folder, new List<string>()
            { "*.wav", "*.ogg"}
        );
    }

    public static List<string> GetAllImageFiles(string folder)
    {
        return GetAllMatchingFiles(folder, new List<string>()
            { "*.png", "*.jpg"}
        );
    }

    public static List<string> GetAllVideoFiles(string folder)
    {
        return GetAllMatchingFiles(folder, new List<string>()
            { "*.mp4"}
        );
    }

    // Removes characters that aren't allowed in file names.
    public static string FilterString(string input)
    {
        StringBuilder builder = new StringBuilder();
        const string invalidChars = "\\/*:?\"<>|";
        foreach (char c in input)
        {
            if (!invalidChars.Contains(c.ToString()))
            {
                builder.Append(c);
            }
        }
        return builder.ToString();
    }

    public static string FilePathToUri(string fullPath)
    {
        return "file://" + fullPath.Replace('\\', '/');
    }

    public const string kTrackFilename = "track.tech";
}
