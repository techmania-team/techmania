using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
        if (trackMetadata.previewStartTime >
            trackMetadata.previewEndTime)
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
        // We could use ResourceLoader.LoadAudio, but this creates
        // problems when the user stops preview track playback
        // before the loading completes.
        string filename = Path.Combine(trackFolder, previewTrackFilename);
        UnityWebRequest request = 
            UnityWebRequestMultimedia.GetAudioClip(
            Paths.FullPathToUri(filename), AudioType.UNKNOWN);
        yield return request.SendWebRequest();

        AudioClip clip;
        string error;
        ResourceLoader.GetAudioClipFromWebRequest(
            request, out clip, out error);
        if (clip == null)
        {
            // When called from SelectPatternDialog, messageDialog
            // is intentionally set to null because we don't support
            // showing 2 dialogs at the same time.
            messageDialog?.Show(error);
            yield break;
        }

        if (startTime < 0f) startTime = 0f;
        if (endTime > clip.length) endTime = clip.length;
        if (endTime == 0f) endTime = clip.length;
        float previewLength = (float)endTime - (float)startTime;

        source.clip = clip;
        source.loop = false;
        source.volume = 0f;
        float fadeLength = 1f;
        if (fadeLength > previewLength * 0.5f)
        {
            fadeLength = previewLength * 0.5f;
        }

        int numLoops = loop ? int.MaxValue : 1;
        for (int i = 0; i < numLoops; i++)
        {
            source.time = (float)startTime;
            source.Play();
            
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
        }
    }

    public void Stop()
    {
        StopAllCoroutines();
        StartCoroutine(InnerStop());
    }

    private IEnumerator InnerStop()
    {
        if (source.volume == 0f) yield break;

        for (; source.volume > 0f; source.volume -= Time.deltaTime * 5f)
        {
            yield return null;
        }
        source.volume = 0f;
        source.Stop();
    }
}
