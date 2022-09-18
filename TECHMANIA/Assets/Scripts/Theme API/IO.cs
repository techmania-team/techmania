using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;

namespace ThemeApi
{
    [MoonSharpUserData]
    public class IO
    {
        public static string LoadTextFileFromTheme(string path)
        {
            return GlobalResource.GetThemeContent<TextAsset>(path)?
                .text;
        }

        // Callback parameter: Status, Texture2D
        public static void LoadTextureFromFile(string path,
            DynValue callback)
        {
            ResourceLoader.LoadImage(path,
                (Status status, Texture2D texture) =>
                {
                    callback.Function.Call(status, texture);
                });
        }

        public static AudioClip LoadAudioFromTheme(string path)
        {
            return GlobalResource.GetThemeContent<AudioClip>(path);
        }

        // Callback parameter: Status, AudioClip
        public static void LoadAudioFromFile(string path,
            DynValue callback)
        {
            ResourceLoader.LoadAudio(path,
                (Status status, AudioClip clip) =>
                {
                    callback.Function.Call(status, clip);
                });
        }

        public static Track LoadFullTrack(string path)
        {
            return Track.LoadFromFile(path) as Track;
        }

        // progressCallback parameter: string (the track currently
        // being loaded)
        // completeCallback parameter: Status
        public static void ReloadTrackList(
            DynValue progressCallback,
            DynValue completeCallback)
        {
            GlobalResourceLoader.GetInstance().LoadTrackList(
                progressCallback: (string currentlyLoadingFile) =>
                {
                    progressCallback.Function.Call(
                        currentlyLoadingFile);
                },
                completeCallback: (Status status) =>
                {
                    completeCallback.Function.Call(status);
                });
        }

        // progressCallback parameter: string (the track currently
        // being loaded / upgraded)
        // completeCallback parameter: Status
        public static void UpgradeAllTracks(
            DynValue progressCallback,
            DynValue completeCallback)
        {
            GlobalResourceLoader.GetInstance().UpdateTrackVersions(
                progressCallback: (string currentlyLoadingFile) =>
                {
                    progressCallback.Function.Call(
                        currentlyLoadingFile);
                },
                completeCallback: (Status status) =>
                {
                    completeCallback.Function.Call(status);
                });
        }
    }
}