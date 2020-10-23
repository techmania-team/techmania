﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class ResourceLoader : MonoBehaviour
{
    private static ResourceLoader GetInstance()
    {
        return FindObjectOfType<ResourceLoader>();
    }

    #region Audio Caching
    // Keys do not contain folder.
    private static Dictionary<string, AudioClip> audioClips;

    // Cache all audio files in the given path.
    public static void CacheAudioResources(string trackFolder,
        UnityAction<string> cacheAudioCompleteCallback)
    {
        ResourceLoader instance = GetInstance();
        instance.StartCoroutine(instance.InnerCacheAudioResources(
            Paths.GetAllAudioFiles(trackFolder),
            cacheAudioCompleteCallback));
    }

    // Cache all keysounds of the given pattern.
    public static void CacheSoundChannels(string trackFolder,
        Pattern pattern, UnityAction<string> cacheAudioCompleteCallback)
    {
        List<string> filenames = new List<string>();
        foreach (SoundChannel channel in pattern.soundChannels)
        {
            if (channel.name != "")
            {
                filenames.Add(trackFolder + "\\" + channel.name);
            }
        }
        ResourceLoader instance = GetInstance();
        instance.StartCoroutine(instance.InnerCacheAudioResources(
            filenames, cacheAudioCompleteCallback));
    }

    private IEnumerator InnerCacheAudioResources(
        List<string> filenameWithFolder,
        UnityAction<string> cacheAudioCompleteCallback)
    {
        audioClips = new Dictionary<string, AudioClip>();

        foreach (string file in filenameWithFolder)
        {
            // string uri = Paths.FilePathToUri(file);
            Debug.Log("Loading: " + file);
            UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(
                file, AudioType.WAV);
            yield return request.SendWebRequest();

            AudioClip clip;
            string error;
            GetAudioClipFromWebRequest(request, out clip, out error);
            if (clip == null)
            {
                cacheAudioCompleteCallback?.Invoke(error);
                yield break;
            }
            else
            {
                audioClips.Add(new FileInfo(file).Name, clip);
                Debug.Log("Loaded: " + file);
            }
        }

        yield return null;  // Wait 1 more frame just in case
        cacheAudioCompleteCallback?.Invoke(null);
    }

    public static AudioClip GetCachedClip(string filenameWithoutFolder)
    {
        if (audioClips.ContainsKey(filenameWithoutFolder))
        {
            return audioClips[filenameWithoutFolder];
        }
        else
        {
            return null;
        }
    }
    #endregion

    #region Audio
    public static void LoadAudio(string fullPath,
        UnityAction<AudioClip, string> loadAudioCompleteCallback)
    {
        ResourceLoader instance = GetInstance();
        instance.StartCoroutine(instance.InnerLoadAudio(
            fullPath, loadAudioCompleteCallback));
    }

    private IEnumerator InnerLoadAudio(string fullPath,
        UnityAction<AudioClip, string> loadAudioCompleteCallback)
    {
        UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(
            fullPath, AudioType.WAV);
        yield return request.SendWebRequest();

        AudioClip clip;
        string error;
        GetAudioClipFromWebRequest(request, out clip, out error);
        if (clip != null)
        {
            Debug.Log("Loaded: " + fullPath);
        }
        loadAudioCompleteCallback?.Invoke(clip, error);
    }

    public static void GetAudioClipFromWebRequest(UnityWebRequest request,
        out AudioClip clip, out string error)
    {
        clip = DownloadHandlerAudioClip.GetContent(request);
        error = null;
        string fullPath = request.uri.LocalPath;
        if (clip == null)
        {
            error = $"Could not load {fullPath}:\n\n{request.error}";
            return;
        }
        if (clip.loadState != AudioDataLoadState.Loaded)
        {
            clip = null;
            error = $"Could not load {fullPath}.\n\n" +
                "The file may be corrupted, or be of an unsupported format.";
            return;
        }
    }
    #endregion

    #region Image
    public static void LoadImage(string fullPath,
        UnityAction<Sprite, string> loadImageCompleteCallback)
    {
        ResourceLoader instance = GetInstance();
        instance.StartCoroutine(instance.InnerLoadImage(
            fullPath, loadImageCompleteCallback));
    }

    private IEnumerator InnerLoadImage(string fullPath,
        UnityAction<Sprite, string> loadImageCompleteCallback)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(
            Paths.FilePathToUri(fullPath), nonReadable: true);
        yield return request.SendWebRequest();

        if (request.isNetworkError || request.isHttpError)
        {
            loadImageCompleteCallback?.Invoke(null,
                $"Could not load {fullPath}:\n\n{request.error}");
            yield break;
        }

        Texture texture = DownloadHandlerTexture.GetContent(request);
        if (texture == null)
        {
            loadImageCompleteCallback?.Invoke(null,
                $"Could not load {fullPath}:\n\n{request.error}");
            yield break;
        }
        Texture2D t2d = texture as Texture2D;
        if (t2d == null)
        {
            loadImageCompleteCallback?.Invoke(null,
                $"Could not load {fullPath} as a 2D image.");
            yield break;
        }

        int width = t2d.width;
        int height = t2d.height;
        Sprite sprite = Sprite.Create(t2d,
            new Rect(0f, 0f, width, height),
            new Vector2(width * 0.5f, height * 0.5f));
        loadImageCompleteCallback.Invoke(sprite, null);
    }
    #endregion
}
