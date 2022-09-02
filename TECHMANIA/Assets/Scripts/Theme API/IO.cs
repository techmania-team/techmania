using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;

namespace ThemeApi
{
    [MoonSharpUserData]
    public class IO
    {
        // Callback parameter: Status, Texture2D
        public static void LoadTexture(string path,
            DynValue callback)
        {
            ResourceLoader.LoadImage(path,
                (Status status, Texture2D texture) =>
                {
                    callback.Function.Call(status, texture);
                });
        }

        // Callback parameter: Status, AudioClip
        public static void LoadAudio(string path,
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