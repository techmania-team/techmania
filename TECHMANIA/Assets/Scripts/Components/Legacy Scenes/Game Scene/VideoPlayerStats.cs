using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Video;

public class VideoPlayerStats : MonoBehaviour
{
    public TextMeshProUGUI text;
    public VideoPlayer video;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        text.text = $"isLooping: {video.isLooping}\nisPaused: {video.isPaused}\nisPlaying: {video.isPlaying}\nisPrepared: {video.isPrepared}\nlength: {video.length}\ntime: {video.time}";
    }
}
