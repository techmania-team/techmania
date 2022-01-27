using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class ResourceLoader : MonoBehaviour
{
    public AudioClip emptyClip;

    private static ResourceLoader GetInstance()
    {
        return FindObjectOfType<ResourceLoader>();
    }

    #region Audio Caching
    // Keys do not contain folder.
    private static Dictionary<string, AudioClip> audioClips;
    // If a cache request is on the same folder, then audioClips
    // will not be cleared.
    private static string cachedFolder;

    // Modifying audio buffer size will unload all clips. Therefore,
    // OptionsPanel should set this flag when the buffer size changes.
    public static bool forceReload;

    static ResourceLoader()
    {
        audioClips = new Dictionary<string, AudioClip>();
        cachedFolder = "";
        forceReload = false;
    }

    private void ReportAudioCache()
    {
        Debug.Log("Cached clips: " + audioClips.Count);
        Debug.Log("Sample sizes:");
        int logs = 0;
        foreach (AudioClip c in audioClips.Values)
        {
            Debug.Log(c.samples);
            logs++;
            if (logs > 5) break;
        }
    }

    private static void ClearAudioCache()
    {
        foreach (AudioClip c in audioClips.Values)
        {
            c.UnloadAudioData();
        }
        audioClips.Clear();
    }

    // Cache all audio files in the given path.
    public static void CacheAudioResources(string trackFolder,
        UnityAction<string> cacheAudioCompleteCallback)
    {
        if (trackFolder != cachedFolder)
        {
            ClearAudioCache();
            cachedFolder = trackFolder;
        }
        if (forceReload)
        {
            ClearAudioCache();
            forceReload = false;
        }

        ResourceLoader instance = GetInstance();
        instance.StartCoroutine(instance.InnerCacheAudioResources(
            trackFolder,
            Paths.GetAllAudioFiles(trackFolder),
            cacheAudioCompleteCallback,
            progressCallback: null));
    }

    // Cache all keysounds of the given pattern.
    public static void CacheAllKeysounds(string trackFolder,
        Pattern pattern,
        UnityAction<string> cacheAudioCompleteCallback,
        UnityAction<float> progressCallback)
    {
        if (trackFolder != cachedFolder)
        {
            ClearAudioCache();
            cachedFolder = trackFolder;
        }
        if (forceReload)
        {
            ClearAudioCache();
            forceReload = false;
        }

        HashSet<string> filenames = new HashSet<string>();
        foreach (Note n in pattern.notes)
        {
            if (n.sound != null && n.sound != "")
            {
                filenames.Add(Path.Combine(trackFolder, n.sound));
            }
        }
        ResourceLoader instance = GetInstance();
        instance.StartCoroutine(instance.InnerCacheAudioResources(
            trackFolder,
            filenames, cacheAudioCompleteCallback,
            progressCallback));
    }

    private IEnumerator InnerCacheAudioResources(
        string trackFolder,
        ICollection<string> filenameWithFolder,
        UnityAction<string> cacheAudioCompleteCallback,
        UnityAction<float> progressCallback)
    {
        Options.TemporarilyDisableVSync();
        int numLoaded = 0;
        foreach (string file in filenameWithFolder)
        {
            string fileRelativePath = Paths.RelativePath(trackFolder, file);
            if (!audioClips.ContainsKey(fileRelativePath))
            {
                // Handle empty files.
                try
                {
                    if (IsEmptyFile(file))
                    {
                        Debug.Log($"{file} is a 0-byte file, loaded as empty clip.");
                        audioClips.Add(fileRelativePath, emptyClip);
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    cacheAudioCompleteCallback?.Invoke(ex.Message);
                    yield break;
                }

                // Somehow passing in AudioType.UNKNOWN will make it
                // magically work for every format.
                UnityWebRequest request =
                    UnityWebRequestMultimedia.GetAudioClip(
                        Paths.FullPathToUri(file), AudioType.UNKNOWN);
                yield return request.SendWebRequest();

                AudioClip clip;
                string error;
                GetAudioClipFromWebRequest(request,
                    out clip, out error);
                if (clip == null)
                {
                    cacheAudioCompleteCallback?.Invoke(error);
                    yield break;
                }
                audioClips.Add(fileRelativePath, clip);
            }
            
            numLoaded++;
            progressCallback?.Invoke((float)numLoaded /
                filenameWithFolder.Count);
            Debug.Log("Loaded: " + file);
        }

        yield return null;  // Wait 1 more frame just in case
        cacheAudioCompleteCallback?.Invoke(null);
        Options.RestoreVSync();
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
        // Handle empty files.
        try
        {
            if (IsEmptyFile(fullPath))
            {
                Debug.Log($"{fullPath} is a 0-byte file, loaded as empty clip.");
                loadAudioCompleteCallback?.Invoke(emptyClip, null);
                yield break;
            }
        }
        catch (Exception ex)
        {
            loadAudioCompleteCallback?.Invoke(null, ex.Message);
            yield break;
        }

        UnityWebRequest request =
            UnityWebRequestMultimedia.GetAudioClip(
            Paths.FullPathToUri(fullPath), AudioType.UNKNOWN);
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

    private static bool IsEmptyFile(string fullPath)
    {
        // FileInfo.Length causes a read, which doesn't work
        // on streaming assets on Android, so we simply bypass
        // the test for streaming assets.
        if (Paths.IsInStreamingAssets(fullPath))
        {
            return false;
        }
        FileInfo fileInfo = new FileInfo(fullPath);
        return fileInfo.Length == 0;
    }

    public static void GetAudioClipFromWebRequest(
        UnityWebRequest request,
        out AudioClip clip, out string error)
    {
        string fullPath = request.uri.LocalPath;
        if (request.result != UnityWebRequest.Result.Success)
        {
            clip = null;
            error = Locale.GetStringAndFormatIncludingPaths(
                "resource_loader_error_format",
                fullPath,
                request.error);
            return;
        }
        clip = DownloadHandlerAudioClip.GetContent(request);
        error = null;

        if (clip == null)
        {
            error = Locale.GetStringAndFormatIncludingPaths(
                "resource_loader_error_format",
                fullPath,
                request.error);
            return;
        }
        if (clip.loadState != AudioDataLoadState.Loaded)
        {
            clip = null;
            error = Locale.GetStringAndFormatIncludingPaths(
                "resource_loader_unsupported_format_error_format",
                fullPath);
            return;
        }
    }
    #endregion

    #region Image
    public static void LoadImage(string fullPath,
        UnityAction<Texture2D, string> loadImageCompleteCallback)
    {
        ResourceLoader instance = GetInstance();
        instance.StartCoroutine(instance.InnerLoadImage(
            fullPath, loadImageCompleteCallback));
    }

    public static Sprite CreateSpriteFromTexture(Texture2D texture)
    {
        return Sprite.Create(texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit: 100f,
            extrude: 0,
            // The default is Tight, whose performance is
            // atrocious.
            meshType: SpriteMeshType.FullRect);
    }

    private IEnumerator InnerLoadImage(string fullPath,
        UnityAction<Texture2D, string> loadImageCompleteCallback)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(
            Paths.FullPathToUri(fullPath), nonReadable: true);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            loadImageCompleteCallback?.Invoke(null,
                Locale.GetStringAndFormatIncludingPaths(
                    "resource_loader_error_format",
                    fullPath,
                    request.error));
            yield break;
        }

        Texture texture = DownloadHandlerTexture.GetContent(request);
        if (texture == null)
        {
            loadImageCompleteCallback?.Invoke(null,
                Locale.GetStringAndFormatIncludingPaths(
                    "resource_loader_error_format",
                    fullPath,
                    request.error));
            yield break;
        }
        Texture2D t2d = texture as Texture2D;
        if (t2d == null)
        {
            loadImageCompleteCallback?.Invoke(null,
                Locale.GetStringAndFormatIncludingPaths(
                    "resource_loader_unsupported_format_error_format",
                    fullPath));
            yield break;
        }

        Debug.Log("Loaded: " + fullPath);
        t2d.wrapMode = TextureWrapMode.Clamp;
        loadImageCompleteCallback.Invoke(t2d, null);
    }
    #endregion
}
