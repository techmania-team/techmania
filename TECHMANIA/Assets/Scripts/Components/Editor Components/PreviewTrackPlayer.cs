using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class PreviewTrackPlayer : MonoBehaviour
{
    public MessageDialog messageDialog;

    private FmodSoundWrap sound;
    private FmodChannelWrap channel;

    // Start is called before the first frame update
    void Start()
    {

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
        string filename = Path.Combine(trackFolder, 
            previewTrackFilename);
        UnityWebRequest request = 
            UnityWebRequestMultimedia.GetAudioClip(
            Paths.FullPathToUri(filename), AudioType.UNKNOWN);
        yield return request.SendWebRequest();

        Status status;
        ResourceLoader.GetSoundFromWebRequest(
            request, out sound, out status);
        if (!status.Ok())
        {
            messageDialog?.Show(status.errorMessage);
            yield break;
        }

        if (startTime < 0f) startTime = 0f;
        if (endTime > sound.length) endTime = sound.length;
        if (endTime == 0f) endTime = sound.length;
        float previewLength = (float)endTime - (float)startTime;

        channel = AudioSourceManager.instance.PlayMusic(sound);
        channel.volume = 0f;
        float fadeLength = 1f;
        if (fadeLength > previewLength * 0.5f)
        {
            fadeLength = previewLength * 0.5f;
        }

        int numLoops = loop ? int.MaxValue : 1;
        for (int i = 0; i < numLoops; i++)
        {
            channel.time = (float)startTime;
            channel.Play();
            
            for (float time = 0f; time < fadeLength; time += Time.deltaTime)
            {
                float progress = time / fadeLength;
                channel.volume = progress;
                yield return null;
            }
            channel.volume = 1f;
            yield return new WaitForSeconds(previewLength - fadeLength * 2f);
            for (float time = 0f; time < fadeLength; time += Time.deltaTime)
            {
                float progress = time / fadeLength;
                channel.volume = 1f - progress;
                yield return null;
            }
            channel.volume = 0f;
        }
    }

    public void Stop()
    {
        StopAllCoroutines();
        StartCoroutine(InnerStop());
    }

    private IEnumerator InnerStop()
    {
        if (channel == null) yield break;
        if (channel.volume == 0f) yield break;

        for (; channel.volume > 0f;
            channel.volume -= Time.deltaTime * 5f)
        {
            yield return null;
        }
        channel.volume = 0f;
        channel.Stop();
        sound.Release();
    }
}
