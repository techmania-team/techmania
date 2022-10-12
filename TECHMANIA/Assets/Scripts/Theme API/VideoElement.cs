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

        private VideoPlayer player;
        private RenderTexture renderTexture;

        #region Creation and Disposal
        [MoonSharpHidden]
        public static VideoElement CreateFromClip(VideoClip clip)
        {
            VideoElement e = new VideoElement();
            e.player = VideoElementManager.InstantiatePlayer();
            e.player.clip = clip;
            e.player.Prepare();
            while (!e.player.isPrepared) { }
            e.PrepareRenderTexture();
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
                e.PrepareRenderTexture();
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

        private void PrepareRenderTexture()
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