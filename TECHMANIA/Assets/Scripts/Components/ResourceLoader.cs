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
    private UnityAction<string> loadCompleteCallback;

    public void LoadResources(string trackPath,
        UnityAction<string> loadCompleteCallback)
    {
        this.loadCompleteCallback = loadCompleteCallback;
        StartCoroutine(InnerLoadResources(trackPath));
    }

    private IEnumerator InnerLoadResources(string trackPath)
    {
        audioClips = new Dictionary<string, AudioClip>();

        string folder = new FileInfo(trackPath).DirectoryName;
        foreach (string file in Paths.GetAllAudioFiles(folder))
        {
            // string uri = Paths.FilePathToUri(file);
            UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(
                file, AudioType.WAV);
            yield return request.SendWebRequest();

            AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
            if (clip == null)
            {
                string error = $"Could not load {file}:\n\n{request.error}";
                loadCompleteCallback?.Invoke(error);
                yield break;
            }
            if (clip.loadState != AudioDataLoadState.Loaded)
            {
                string error = $"Could not load {file}.\n\n" +
                    "The file may be corrupted, or be of an unsupported format.";
                loadCompleteCallback?.Invoke(error);
                yield break;
            }

            audioClips.Add(new FileInfo(file).Name, clip);
            Debug.Log("Loaded: " + file);
        }

        yield return null;  // Wait 1 more frame just in case
        loadCompleteCallback?.Invoke(null);
    }

    public AudioClip GetClip(string filenameWithoutFolder)
    {
        return audioClips[filenameWithoutFolder];
    }
}
