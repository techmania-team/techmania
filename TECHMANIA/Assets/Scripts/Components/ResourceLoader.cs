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
                Debug.LogError(request.error);
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
}
