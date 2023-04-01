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
                    if (callback.IsNil()) return;
                    callback?.Function.Call(status, texture);
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
                    if (callback.IsNil()) return;
                    callback.Function.Call(status, clip);
                });
        }

        public static VideoElement LoadVideoFromTheme(string path)
        {
            UnityEngine.Video.VideoClip clip =
                GlobalResource.GetThemeContent<
                    UnityEngine.Video.VideoClip>(path);
            return VideoElement.CreateFromClip(clip);
        }

        // Callback parameters: Status, VideoElement
        public static void LoadVideoFromFile(string path,
            DynValue callback)
        {
            VideoElement.CreateFromFile(path,
                callback: (Status status, VideoElement element) =>
                {
                    if (callback.IsNil()) return;
                    callback.Function.Call(status, element);
                });
        }

        public static UnityEngine.TextCore.Text.FontAsset
            LoadFontFromTheme(string path)
        {
            return GlobalResource.GetThemeContent
                <UnityEngine.TextCore.Text.FontAsset>(path);
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
                    if (progressCallback.IsNil()) return;
                    progressCallback.Function.Call(
                        currentlyLoadingFile);
                },
                completeCallback: (Status status) =>
                {
                    if (completeCallback.IsNil()) return;
                    completeCallback?.Function.Call(status);
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
                    if (progressCallback.IsNil()) return;
                    progressCallback.Function.Call(
                        currentlyLoadingFile);
                },
                completeCallback: (Status status) =>
                {
                    if (completeCallback.IsNil()) return;
                    completeCallback?.Function.Call(status);
                });
        }

        public static void ReloadNoteSkin(
            DynValue progressCallback,
            DynValue completeCallback)
        {
            GlobalResourceLoader.GetInstance().LoadNoteSkin(
                progressCallback: (string currentlyLoadingFile) =>
                {
                    if (progressCallback.IsNil()) return;
                    progressCallback.Function.Call(
                        currentlyLoadingFile);
                },
                completeCallback: (Status status) =>
                {
                    if (completeCallback.IsNil()) return;
                    completeCallback?.Function.Call(status);
                });
        }

        public static void ReloadVfxSkin(
            DynValue progressCallback,
            DynValue completeCallback)
        {
            GlobalResourceLoader.GetInstance().LoadVfxSkin(
                progressCallback: (string currentlyLoadingFile) =>
                {
                    if (progressCallback.IsNil()) return;
                    progressCallback.Function.Call(
                        currentlyLoadingFile);
                },
                completeCallback: (Status status) =>
                {
                    if (completeCallback.IsNil()) return;
                    completeCallback?.Function.Call(status);
                });
        }

        public static void ReloadComboSkin(
            DynValue progressCallback,
            DynValue completeCallback)
        {
            GlobalResourceLoader.GetInstance().LoadComboSkin(
                progressCallback: (string currentlyLoadingFile) =>
                {
                    if (progressCallback.IsNil()) return;
                    progressCallback.Function.Call(
                        currentlyLoadingFile);
                },
                completeCallback: (Status status) =>
                {
                    if (completeCallback.IsNil()) return;
                    completeCallback?.Function.Call(status);
                });
        }

        public static void ReloadGameUiSkin(
            DynValue progressCallback,
            DynValue completeCallback)
        {
            GlobalResourceLoader.GetInstance().LoadGameUiSkin(
                progressCallback: (string currentlyLoadingFile) =>
                {
                    if (progressCallback.IsNil()) return;
                    progressCallback.Function.Call(
                        currentlyLoadingFile);
                },
                completeCallback: (Status status) =>
                {
                    if (completeCallback.IsNil()) return;
                    completeCallback?.Function.Call(status);
                });
        }
    }
}