using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class ResourceLoader : MonoBehaviour
{
    private static ResourceLoader instance;

    public AudioClip emptyClip;

    private static ResourceLoader GetInstance()
    {
        if (instance == null)
        {
            instance = FindObjectOfType<ResourceLoader>();
        }
        return instance;
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
        UnityAction<Status> cacheAudioCompleteCallback)
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

        GetInstance().StartCoroutine(
            GetInstance().InnerCacheAudioResources(
                trackFolder,
                Paths.GetAllAudioFiles(trackFolder),
                cacheAudioCompleteCallback,
                progressCallback: null));
    }

    // Cache all keysounds of the given pattern.
    public static void CacheAllKeysounds(string trackFolder,
        Pattern pattern,
        UnityAction<Status> cacheAudioCompleteCallback,
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
        GetInstance().StartCoroutine(
            GetInstance().InnerCacheAudioResources(
                trackFolder,
                filenames, cacheAudioCompleteCallback,
                progressCallback));
    }

    private IEnumerator InnerCacheAudioResources(
        string trackFolder,
        ICollection<string> filenameWithFolder,
        UnityAction<Status> cacheAudioCompleteCallback,
        UnityAction<float> progressCallback)
    {
        Options.TemporarilyDisableVSync();
        int numLoaded = 0;
        foreach (string file in filenameWithFolder)
        {
            string fileRelativePath = Paths.RelativePath(
                trackFolder, file);
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
                    cacheAudioCompleteCallback?.Invoke(
                        Status.FromException(ex));
                    yield break;
                }

                // Somehow passing in AudioType.UNKNOWN will make it
                // magically work for every format.
                UnityWebRequest request =
                    UnityWebRequestMultimedia.GetAudioClip(
                        Paths.FullPathToUri(file), AudioType.UNKNOWN);
                yield return request.SendWebRequest();

                AudioClip clip;
                Status status;
                GetAudioClipFromWebRequest(request,
                    out clip, out status);
                if (!status.Ok())
                {
                    cacheAudioCompleteCallback?.Invoke(status);
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
        cacheAudioCompleteCallback?.Invoke(Status.OKStatus());
        Options.RestoreVSync();
    }

    public static AudioClip GetCachedClip(
        string filenameWithoutFolder)
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
    public delegate void AudioLoadCompleteCallback(
        Status status, AudioClip clip = null);

    public static void LoadAudio(string fullPath,
        AudioLoadCompleteCallback loadAudioCompleteCallback)
    {
        GetInstance().StartCoroutine(GetInstance().InnerLoadAudio(
            fullPath, loadAudioCompleteCallback));
    }

    private IEnumerator InnerLoadAudio(string fullPath,
        AudioLoadCompleteCallback loadAudioCompleteCallback)
    {
        // Handle empty files.
        try
        {
            if (IsEmptyFile(fullPath))
            {
                Debug.Log($"{fullPath} is a 0-byte file, loaded as empty clip.");
                loadAudioCompleteCallback?.Invoke(
                    Status.OKStatus(), emptyClip);
                yield break;
            }
        }
        catch (Exception ex)
        {
            loadAudioCompleteCallback?.Invoke(
                Status.FromException(ex), null);
            yield break;
        }

        UnityWebRequest request =
            UnityWebRequestMultimedia.GetAudioClip(
            Paths.FullPathToUri(fullPath), AudioType.UNKNOWN);
        yield return request.SendWebRequest();

        AudioClip clip;
        Status status;
        GetAudioClipFromWebRequest(request, out clip, out status);
        if (clip != null)
        {
            Debug.Log("Loaded: " + fullPath);
        }
        loadAudioCompleteCallback?.Invoke(status, clip);
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
        out AudioClip clip, out Status status)
    {
        string fullPath = request.uri.LocalPath;
        if (request.result != UnityWebRequest.Result.Success)
        {
            clip = null;
            status = Status.Error(Status.Code.OtherError,
                request.error, fullPath);
            return;
        }
        clip = DownloadHandlerAudioClip.GetContent(request);

        if (clip == null)
        {
            status = Status.Error(Status.Code.OtherError,
                request.error, fullPath);
            return;
        }
        if (clip.loadState != AudioDataLoadState.Loaded)
        {
            clip = null;
            status = Status.Error(Status.Code.OtherError,
                request.error, fullPath);
            return;
        }
        status = Status.OKStatus();
    }
    #endregion

    #region Image
    public delegate void ImageLoadCompleteCallback(
        Status status, Texture2D texture = null);

    public static void LoadImage(string fullPath,
        ImageLoadCompleteCallback completeCallback)
    {
        GetInstance().StartCoroutine(GetInstance().InnerLoadImage(
            fullPath, completeCallback));
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
        ImageLoadCompleteCallback loadImageCompleteCallback)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(
            Paths.FullPathToUri(fullPath), nonReadable: true);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            loadImageCompleteCallback?.Invoke(
                Status.Error(Status.Code.OtherError,
                request.error, fullPath));
            yield break;
        }
        Texture texture = DownloadHandlerTexture.GetContent(request);
        if (texture == null)
        {
            loadImageCompleteCallback?.Invoke(
                Status.Error(Status.Code.OtherError,
                request.error, fullPath));
            yield break;
        }
        Texture2D t2d = texture as Texture2D;
        if (t2d == null)
        {
            loadImageCompleteCallback?.Invoke(
                Status.Error(Status.Code.OtherError,
                request.error, fullPath));
            yield break;
        }

        Debug.Log("Loaded: " + fullPath);
        t2d.wrapMode = TextureWrapMode.Clamp;
        loadImageCompleteCallback.Invoke(Status.OKStatus(),
            texture: t2d);
    }
    #endregion
}
