using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PreviewTrackPlayer : MonoBehaviour
{
    public MessageDialog messageDialog;

    private AudioSource source;

    // Start is called before the first frame update
    void Start()
    {
        source = GetComponent<AudioSource>();
    }

    public void Play(string trackFolder,
        TrackMetadata trackMetadata,
        bool loop)
    {
        if (trackMetadata.previewTrack == "" ||
            trackMetadata.previewTrack == null)
        {
            return;
        }

        StopAllCoroutines();
        StartCoroutine(InnerPlay(trackFolder,
            trackMetadata.previewTrack,
            trackMetadata.previewStartTime,
            trackMetadata.previewEndTime,
            loop));
    }

    private IEnumerator InnerPlay(string trackFolder,
        string previewTrackFilename,
        double startTime, double endTime,
        bool loop)
    {
        string filename = trackFolder + "\\" + previewTrackFilename;
        UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(
                filename, AudioType.WAV);
        yield return request.SendWebRequest();

        AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
        if (clip == null)
        {
            string error = $"Could not load {filename}:\n\n{request.error}";
            messageDialog?.Show(error);
            yield break;
        }
        if (clip.loadState != AudioDataLoadState.Loaded)
        {
            string error = $"Could not load {filename}.\n\n" +
                "The file may be corrupted, or be of an unsupported format.";
            messageDialog?.Show(error);
            yield break;
        }

        if (startTime < 0f) startTime = 0f;
        if (endTime > clip.length) endTime = clip.length;
        float previewLength = (float)endTime - (float)startTime;

        source.clip = clip;
        source.loop = loop;
        source.volume = 0f;
        source.time = (float)startTime;
        source.Play();

        float fadeLength = 1f;
        if (fadeLength > previewLength * 0.5f)
        {
            fadeLength = previewLength * 0.5f;
        }
        for (float time = 0f; time < fadeLength; time += Time.deltaTime)
        {
            float progress = time / fadeLength;
            source.volume = progress;
            yield return null;
        }
        source.volume = 1f;
        yield return new WaitForSeconds(previewLength - fadeLength * 2f);
        for (float time = 0f; time < fadeLength; time += Time.deltaTime)
        {
            float progress = time / fadeLength;
            source.volume = 1f - progress;
            yield return null;
        }
        source.volume = 0f;
        source.Stop();
    }

    public void Stop()
    {
        source.Stop();
    }
}
