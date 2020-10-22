using System.Collections;
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
    private static UnityAction<string> cacheAudioCompleteCallback;

    // Cache all audio files in the given path.
    public static void CacheAudioResources(string trackFolder,
        UnityAction<string> cacheAudioCompleteCallback)
    {
        ResourceLoader.cacheAudioCompleteCallback =
            cacheAudioCompleteCallback;
        GetInstance().StartCoroutine(
            GetInstance().InnerCacheAudioResources(
                Paths.GetAllAudioFiles(trackFolder)));
    }

    // Cache the backing track and all keysounds of the given
    // pattern.
    public static void CacheAudioResources(string trackFolder,
        Pattern pattern, UnityAction<string> cacheAudioCompleteCallback)
    {
        ResourceLoader.cacheAudioCompleteCallback = cacheAudioCompleteCallback;
        List<string> filenames = new List<string>();
        if (pattern.patternMetadata.backingTrack != null &&
            pattern.patternMetadata.backingTrack != "")
        {
            filenames.Add(trackFolder + "\\" + pattern.patternMetadata.backingTrack);
        }
        foreach (SoundChannel channel in pattern.soundChannels)
        {
            filenames.Add(trackFolder + "\\" + channel.name);
        }
        GetInstance().StartCoroutine(
            GetInstance().InnerCacheAudioResources(filenames));
    }

    private IEnumerator InnerCacheAudioResources(List<string> filenameWithFolder)
    {
        audioClips = new Dictionary<string, AudioClip>();

        foreach (string file in filenameWithFolder)
        {
            // string uri = Paths.FilePathToUri(file);
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
    private static UnityAction<AudioClip, string> loadAudioCompleteCallback;

    public static void LoadAudio(string fullPath,
        UnityAction<AudioClip, string> loadAudioCompleteCallback)
    {
        ResourceLoader.loadAudioCompleteCallback = loadAudioCompleteCallback;
        GetInstance().StartCoroutine(
            GetInstance().InnerLoadAudio(fullPath));
    }

    private IEnumerator InnerLoadAudio(string fullPath)
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
}
