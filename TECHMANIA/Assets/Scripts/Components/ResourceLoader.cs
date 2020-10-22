using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class ResourceLoader : MonoBehaviour
{
    // Keys do not contain folder.
    private Dictionary<string, AudioClip> audioClips;
    private UnityAction<string> loadAudioCompleteCallback;

    // Load all audio files in the given path.
    public void LoadAudioResources(string trackFolder,
        UnityAction<string> loadAudioCompleteCallback)
    {
        this.loadAudioCompleteCallback = loadAudioCompleteCallback;
        StartCoroutine(InnerLoadAudioResources(
            Paths.GetAllAudioFiles(trackFolder)));
    }

    // Load the backing track and all keysounds of the given
    // pattern.
    public void LoadAudioResources(string trackFolder,
        Pattern pattern, UnityAction<string> loadAudioCompleteCallback)
    {
        this.loadAudioCompleteCallback = loadAudioCompleteCallback;
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
        StartCoroutine(InnerLoadAudioResources(filenames));
    }

    private IEnumerator InnerLoadAudioResources(List<string> filenameWithFolder)
    {
        audioClips = new Dictionary<string, AudioClip>();

        foreach (string file in filenameWithFolder)
        {
            // string uri = Paths.FilePathToUri(file);
            UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(
                file, AudioType.WAV);
            yield return request.SendWebRequest();

            AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
            if (clip == null)
            {
                string error = $"Could not load {file}:\n\n{request.error}";
                loadAudioCompleteCallback?.Invoke(error);
                yield break;
            }
            if (clip.loadState != AudioDataLoadState.Loaded)
            {
                string error = $"Could not load {file}.\n\n" +
                    "The file may be corrupted, or be of an unsupported format.";
                loadAudioCompleteCallback?.Invoke(error);
                yield break;
            }

            audioClips.Add(new FileInfo(file).Name, clip);
            Debug.Log("Loaded: " + file);
        }

        yield return null;  // Wait 1 more frame just in case
        loadAudioCompleteCallback?.Invoke(null);
    }

    public AudioClip GetClip(string filenameWithoutFolder)
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
}
