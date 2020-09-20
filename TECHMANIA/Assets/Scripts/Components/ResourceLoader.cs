using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
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
    private bool error;
    // Keys do not contain folder.
    private Dictionary<string, AudioClip> audioClips;

    public void LoadResources(string trackPath)
    {
        StartCoroutine(InnerLoadResources(trackPath));
    }

    private IEnumerator InnerLoadResources(string trackPath)
    {
        loading = true;
        error = false;
        audioClips = new Dictionary<string, AudioClip>();

        string folder = new FileInfo(trackPath).DirectoryName;
        foreach (string file in Paths.GetAllAudioFiles(folder))
        {
            string uri = Paths.FilePathToUri(file);
            UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(
                file, AudioType.WAV);
            yield return request.SendWebRequest();

            AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
            if (clip == null)
            {
                // MessageDialog.Show($"Could not load {file}.\n\nDetails:\n{request.error}");
                error = true;
                yield break;
            }
            if (clip.loadState != AudioDataLoadState.Loaded)
            {
                // MessageDialog.Show($"Could not load {file}.\n\nThe file may be corrupted, or be of an unsupported format.");
                error = true;
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

    public bool AnyErrorOccurred()
    {
        return error;
    }

    public AudioClip GetClip(string filenameWithoutFolder)
    {
        return audioClips[filenameWithoutFolder];
    }
}
