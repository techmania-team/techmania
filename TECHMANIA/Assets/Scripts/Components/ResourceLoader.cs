using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class ResourceLoader : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private bool loading;
    // Keys do not contain folder.
    private Dictionary<string, AudioClip> audioClips;

    public void LoadResources()
    {
        StartCoroutine(InnerLoadResources());
    }

    private IEnumerator InnerLoadResources()
    {
        loading = true;
        audioClips = new Dictionary<string, AudioClip>();

        string folder = new FileInfo(Navigation.GetCurrentTrackPath()).DirectoryName;
        foreach (string file in Directory.EnumerateFiles(folder, "*.wav"))
        {
            string uri = "file://" + file.Replace('\\', '/');
            UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(
                file, AudioType.WAV);
            yield return request.SendWebRequest();

            AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
            if (clip == null)
            {
                MessageDialog.Show($"Could not load {file}.\n\nDetails:\n{request.error}");
                yield break;
            }
            if (clip.loadState != AudioDataLoadState.Loaded)
            {
                MessageDialog.Show($"Could not load {file}.\n\nThe file may be corrupted, or be of an unsupported format.");
                yield break;
            }

            audioClips.Add(new FileInfo(file).Name, clip);
            Debug.Log("Loaded: " + file);
        }

        loading = false;
    }

    public bool LoadComplete()
    {
        return !loading;
    }

    public AudioClip GetClip(string filenameWithoutFolder)
    {
        return audioClips[filenameWithoutFolder];
    }
}
