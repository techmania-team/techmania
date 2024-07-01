using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;

namespace ThemeApi
{
    [MoonSharpUserData]
    public static class IO
    {
        #region Assets from file
        private static HashSet<Texture2D> texturesFromFile;
        private static HashSet<FmodSoundWrap> soundsFromFile;
        private static HashSet<VideoElement> videosFromFile;
        static IO()
        {
            texturesFromFile = new HashSet<Texture2D>();
            soundsFromFile = new HashSet<FmodSoundWrap>();
            videosFromFile = new HashSet<VideoElement>();
        }

        public static int numTexturesFromFile => 
            texturesFromFile.Count;
        public static int numSoundsFromFile => soundsFromFile.Count;
        public static int numVideosFromFile => videosFromFile.Count;
        #endregion

        public static bool FileExists(string path)
        {
            return UniversalIO.FileExists(path);
        }

        public static string LoadTextFileFromTheme(string path)
        {
            return GlobalResource.GetThemeContent<TextAsset>(path)?
                .text;
        }

        public static Texture2D LoadTextureFromTheme(string path)
        {
            return GlobalResource.GetThemeContent<Texture2D>(path);
        }

        // Callback parameter: Status, Texture2D
        public static void LoadTextureFromFile(string path,
            DynValue callback)
        {
            ResourceLoader.LoadImage(path,
                (Status status, Texture2D texture) =>
                {
                    if (status.Ok())
                    {
                        texturesFromFile.Add(texture);
                    }
                    if (callback.IsNil()) return;
                    callback.Function.Call(status, texture);
                });
        }

        public static void ReleaseTexture(Texture2D texture)
        {
            if (texturesFromFile.Contains(texture))
            {
                texturesFromFile.Remove(texture);
            }
            else
            {
                Debug.LogWarning("The texture being released was not loaded from a file. This may cause issues in the theme.");
            }
            Object.Destroy(texture);
        }

        public static FmodSoundWrap LoadAudioFromTheme(string path)
        {
            return GlobalResource.GetThemeContent<FmodSoundWrap>(path);
        }

        // Callback parameter: Status, FmodSoundWrap
        public static void LoadAudioFromFile(string path,
            DynValue callback)
        {
            ResourceLoader.LoadAudio(path,
                (Status status, FmodSoundWrap sound) =>
                {
                    if (status.Ok())
                    {
                        soundsFromFile.Add(sound);
                    }
                    if (callback.IsNil()) return;
                    callback.Function.Call(status, sound);
                });
        }

        public static void ReleaseAudio(FmodSoundWrap sound)
        {
            if (soundsFromFile.Contains(sound))
            {
                soundsFromFile.Remove(sound);
            }
            else
            {
                Debug.LogWarning("The sound being released was not loaded from a file. This may cause issues in the theme.");
            }
            sound.Release();
        }

        // Callback parameter: VideoElement
        public static void LoadVideoFromTheme(string path,
            DynValue callback)
        {
            UnityEngine.Video.VideoClip clip =
                GlobalResource.GetThemeContent<
                    UnityEngine.Video.VideoClip>(path);
            VideoElement.CreateFromClip(clip,
                callback: (VideoElement element) =>
                {
                    if (callback.IsNil()) return;
                    callback.Function.Call(element);
                });
        }

        // Callback parameters: Status, VideoElement
        public static void LoadVideoFromFile(string path,
            DynValue callback)
        {
            VideoElement.CreateFromFile(path,
                callback: (Status status, VideoElement element) =>
                {
                    if (status.Ok())
                    {
                        videosFromFile.Add(element);
                    }
                    if (callback.IsNil()) return;
                    callback.Function.Call(status, element);
                });
        }

        // This should be called on all videos that the theme no
        // longer needs, whether loaded from theme or file.
        public static void ReleaseVideo(VideoElement video)
        {
            if (videosFromFile.Contains(video))
            {
                videosFromFile.Remove(video);
            }
            video.Release();
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
                    completeCallback.Function.Call(status);
                },
                upgradeVersion: false);
        }

        // progressCallback parameter: string (the track currently
        // being loaded / upgraded)
        // completeCallback parameter: Status
        public static void UpgradeAllTracks(
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
                    completeCallback.Function.Call(status);
                },
                upgradeVersion: true);
        }

        // progressCallback parameter: string (the setlist currently
        // being loaded)
        // completeCallback parameter: Status
        public static void ReloadSetlistList(
            DynValue progressCallback,
            DynValue completeCallback)
        {
            GlobalResourceLoader.GetInstance().LoadSetlistList(
                progressCallback: (string currentlyLoadingFile) =>
                {
                    if (progressCallback.IsNil()) return;
                    progressCallback.Function.Call(
                        currentlyLoadingFile);
                },
                completeCallback: (Status status) =>
                {
                    if (completeCallback.IsNil()) return;
                    completeCallback.Function.Call(status);
                },
                upgradeVersion: false);
        }

        // progressCallback parameter: string (the setlist currently
        // being loaded)
        // completeCallback parameter: Status
        public static void UpgradeAllSetlists(
            DynValue progressCallback,
            DynValue completeCallback)
        {
            GlobalResourceLoader.GetInstance().LoadSetlistList(
                progressCallback: (string currentlyLoadingFile) =>
                {
                    if (progressCallback.IsNil()) return;
                    progressCallback.Function.Call(
                        currentlyLoadingFile);
                },
                completeCallback: (Status status) =>
                {
                    if (completeCallback.IsNil()) return;
                    completeCallback.Function.Call(status);
                },
                upgradeVersion: true);
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
                    completeCallback.Function.Call(status);
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
                    completeCallback.Function.Call(status);
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
                    completeCallback.Function.Call(status);
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
                    completeCallback.Function.Call(status);
                });
        }

        public static void ReloadAllSkins(
            DynValue progressCallback,
            DynValue completeCallback)
        {
            GlobalResourceLoader.GetInstance().LoadAllSkins(
                progressCallback: (string currentlyLoadingFile) =>
                {
                    if (progressCallback.IsNil()) return;
                    progressCallback.Function.Call(
                        currentlyLoadingFile);
                },
                completeCallback: (Status status) =>
                {
                    if (completeCallback.IsNil()) return;
                    completeCallback.Function.Call(status);
                });
        }
    }
}

