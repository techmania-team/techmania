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
    private FmodSoundWrap emptySound;

    private static ResourceLoader GetInstance()
    {
        if (instance == null)
        {
            instance = FindObjectOfType<ResourceLoader>();
            instance.emptySound = FmodManager
                .CreateSoundFromAudioClip(
                instance.emptyClip);
        }
        return instance;
    }

    #region Audio Caching
    // Keys do not contain folder.
    private static Dictionary<string, FmodSoundWrap> sounds;
    // If a cache request is on the same folder, then audioClips
    // will not be cleared.
    private static string cachedFolder;

    // Modifying audio buffer size will unload all clips. Therefore,
    // OptionsPanel should set this flag when the buffer size changes.
    public static bool forceReload;

    static ResourceLoader()
    {
        sounds = new Dictionary<string, FmodSoundWrap>();
        cachedFolder = "";
        forceReload = false;
    }

    private void ReportAudioCache()
    {
        Debug.Log("Cached clips: " + sounds.Count);
        Debug.Log("Sample sizes:");
        int logs = 0;
        foreach (FmodSoundWrap s in sounds.Values)
        {
            Debug.Log(s.samples);
            logs++;
            if (logs > 5) break;
        }
    }

    private static void ClearAudioCache()
    {
        foreach (FmodSoundWrap s in sounds.Values)
        {
            s.Release();
        }
        sounds.Clear();
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
                fileLoadedCallback: null));
    }

    // Cache all keysounds of the given pattern.
    public static void CacheAllKeysounds(string trackFolder,
        Pattern pattern,
        UnityAction<Status> cacheAudioCompleteCallback,
        UnityAction<string> fileLoadedCallback)
    {
        HashSet<string> filenames = new HashSet<string>();
        foreach (Note n in pattern.notes)
        {
            if (n.sound != null && n.sound != "")
            {
                filenames.Add(Path.Combine(trackFolder, n.sound));
            }
        }
        CacheAllKeysounds(trackFolder, filenames,
            cacheAudioCompleteCallback,
            fileLoadedCallback);
    }

    public static void CacheAllKeysounds(string trackFolder,
        HashSet<string> keysoundFullPaths,
        UnityAction<Status> cacheAudioCompleteCallback,
        UnityAction<string> fileLoadedCallback)
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
               keysoundFullPaths,
               cacheAudioCompleteCallback,
               fileLoadedCallback));
    }

    private IEnumerator InnerCacheAudioResources(
        string trackFolder,
        ICollection<string> filenameWithFolder,
        UnityAction<Status> cacheAudioCompleteCallback,
        UnityAction<string> fileLoadedCallback)
    {
        Options.TemporarilyDisableVSync();
        int numLoaded = 0;
        foreach (string file in filenameWithFolder)
        {
            string fileRelativePath = Paths.RelativePath(
                trackFolder, file);
            if (!sounds.ContainsKey(fileRelativePath))
            {
                // Handle empty files.
                try
                {
                    if (IsEmptyFile(file))
                    {
                        Debug.Log($"{file} is a 0-byte file, loaded as empty sound.");
                        sounds.Add(fileRelativePath, emptySound);
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

                FmodSoundWrap sound;
                Status status;
                GetSoundFromWebRequest(request,
                    out sound, out status);
                if (!status.Ok())
                {
                    cacheAudioCompleteCallback?.Invoke(status);
                    yield break;
                }
                sounds.Add(fileRelativePath, sound);
            }
            
            numLoaded++;
            fileLoadedCallback?.Invoke(fileRelativePath);
        }

        yield return null;  // Wait 1 more frame just in case
        cacheAudioCompleteCallback?.Invoke(Status.OKStatus());
        Options.RestoreVSync();
    }

    public static FmodSoundWrap GetCachedSound(
        string filenameWithoutFolder)
    {
        if (sounds.ContainsKey(filenameWithoutFolder))
        {
            return sounds[filenameWithoutFolder];
        }
        else
        {
            return null;
        }
    }
    #endregion

    #region Audio
    public delegate void AudioLoadCompleteCallback(
        Status status, FmodSoundWrap sound);

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
                    Status.OKStatus(), emptySound);
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

        FmodSoundWrap sound;
        Status status;
        GetSoundFromWebRequest(request, out sound, out status);
        if (sound != null)
        {
            Debug.Log("Loaded: " + fullPath);
        }
        loadAudioCompleteCallback?.Invoke(status, sound);
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

    public static void GetSoundFromWebRequest(
        UnityWebRequest request,
        out FmodSoundWrap clip, out Status status)
    {
        clip = null;
        string fullPath = request.uri.LocalPath;
        if (request.result != UnityWebRequest.Result.Success)
        {
            clip = null;
            status = Status.Error(Status.Code.OtherError,
                request.error, fullPath);
            return;
        }
        AudioClip clipFromRequest = DownloadHandlerAudioClip
            .GetContent(request);

        if (clipFromRequest == null)
        {
            status = Status.Error(Status.Code.OtherError,
                request.error, fullPath);
            return;
        }
        if (clipFromRequest.loadState != AudioDataLoadState.Loaded)
        {
            status = Status.Error(Status.Code.OtherError,
                request.error, fullPath);
            return;
        }
        clip = FmodManager.CreateSoundFromAudioClip(clipFromRequest);
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

        t2d.wrapMode = TextureWrapMode.Clamp;
        loadImageCompleteCallback.Invoke(Status.OKStatus(),
            texture: t2d);
    }
    #endregion
}
