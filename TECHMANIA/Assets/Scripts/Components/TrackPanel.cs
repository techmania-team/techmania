using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class TrackPanel : MonoBehaviour
{
    public static Track currentTrack;

    public TextField title;
    public TextField artist;
    public TextField genre;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnEnable()
    {

    }

    public void UIToMemory()
    {
        currentTrack.trackMetadata.title = title.value;
        currentTrack.trackMetadata.artist = artist.value;
        currentTrack.trackMetadata.genre = genre.value;
    }

    public void MemoryToUI()
    {
        title.value = currentTrack.trackMetadata.title;
        artist.value = currentTrack.trackMetadata.artist;
        genre.value = currentTrack.trackMetadata.genre;
    }
}
