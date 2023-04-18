using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using MoonSharp.Interpreter;

namespace ThemeApi
{
    // A wrapper around VideoPlayer that plays video on a
    // VisualElement.
    [MoonSharpUserData]
    public class VideoElement
    {
        public VisualElementWrap targetElement;

        public VideoPlayer player { get; private set; }
        public RenderTexture renderTexture { get; private set; }

        #region Creation and Disposal
        [MoonSharpHidden]
        public static VideoElement CreateFromClip(VideoClip clip)
        {
            VideoElement e = new VideoElement();
            e.player = VideoElementManager.InstantiatePlayer();
            e.player.clip = clip;
            e.player.Prepare();
            e.PrepareToPlay();
            return e;
        }

        [MoonSharpHidden]
        public static void CreateFromFile(string path,
            System.Action<Status, VideoElement> callback)
        {
            VideoElement e = new VideoElement();
            e.player = VideoElementManager.InstantiatePlayer();
            e.player.url = path;
            e.player.prepareCompleted += (VideoPlayer source) =>
            {
                e.PrepareToPlay();
                callback(Status.OKStatus(), e);
            };
            e.player.errorReceived += (
                VideoPlayer source, string message) =>
            {
                e.Dispose();
                callback(Status.Error(
                    Status.Code.IOError, message, path),
                    null);
            };
            e.player.Prepare();
        }

        private void PrepareToPlay()
        {
            renderTexture = new RenderTexture(
                width: (int)player.width,
                height: (int)player.height,
                depth: 16);
            player.targetTexture = renderTexture;
        }

        public void Dispose()
        {
            if (renderTexture != null &&
                renderTexture.IsCreated())
            {
                renderTexture.Release();
            }
            VideoElementManager.DestroyPlayer(player);
        }
        #endregion

        #region Properties
        public float length => (float)player.length;
        public float time
        {
            get { return (float)player.time; }
            set { player.time = value; }
        }

        public bool isPlaying => player.isPlaying;
        public bool isPaused => player.isPaused;
        public bool isLooping
        {
            get { return player.isLooping; }
            set { player.isLooping = value; }
        }
        #endregion

        #region Controls
        public void Play()
        {
            targetElement.inner.style.backgroundImage =
                new UnityEngine.UIElements.StyleBackground(
                    UnityEngine.UIElements.Background
                    .FromRenderTexture(renderTexture));
            player.Play();
        }

        public void Pause() => player.Pause();

        public void Unpause() => player.Play();

        public void Stop() => player.Stop();
        #endregion
    }
}