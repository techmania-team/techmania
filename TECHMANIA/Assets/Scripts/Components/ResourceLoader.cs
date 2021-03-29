using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
            if (n.sound != "")
            {
                filenames.Add(Path.Combine(trackFolder, n.sound));
            }
        }
        ResourceLoader instance = GetInstance();
        instance.StartCoroutine(instance.InnerCacheAudioResources(
            filenames, cacheAudioCompleteCallback,
            progressCallback));
    }

    private IEnumerator InnerCacheAudioResources(
        ICollection<string> filenameWithFolder,
        UnityAction<string> cacheAudioCompleteCallback,
        UnityAction<float> progressCallback)
    {
        int numLoaded = 0;
        foreach (string file in filenameWithFolder)
        {
            string fileWithoutFolder = new FileInfo(file).Name;
            if (!audioClips.ContainsKey(fileWithoutFolder))
            {
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
                audioClips.Add(fileWithoutFolder, clip);
            }
            
            numLoaded++;
            progressCallback?.Invoke((float)numLoaded /
                filenameWithFolder.Count);
            Debug.Log("Loaded: " + file);
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

    public static void GetAudioClipFromWebRequest(
        UnityWebRequest request,
        out AudioClip clip, out string error)
    {
        string fullPath = request.uri.LocalPath;
        if (request.result != UnityWebRequest.Result.Success)
        {
            clip = null;
            error = $"Could not load {fullPath}:\n\n{request.error}";
            return;
        }
        clip = DownloadHandlerAudioClip.GetContent(request);
        error = null;

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
            new Vector2(0.5f, 0.5f));
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

        Debug.Log("Loaded: " + fullPath);
        loadImageCompleteCallback.Invoke(t2d, null);
    }
    #endregion
}
