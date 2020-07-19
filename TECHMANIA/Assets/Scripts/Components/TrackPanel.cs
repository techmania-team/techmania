using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TrackPanel : MonoBehaviour
{
    public InputField title;
    public InputField artist;
    public InputField genre;

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
        MemoryToUI();
    }

    public void UIToMemory()
    {
        Navigation.GetCurrentTrack().trackMetadata.title = title.text;
        Navigation.GetCurrentTrack().trackMetadata.artist = artist.text;
        Navigation.GetCurrentTrack().trackMetadata.genre = genre.text;

        Navigation.SetDirty();
    }

    public void MemoryToUI()
    {
        title.text = Navigation.GetCurrentTrack().trackMetadata.title;
        artist.text = Navigation.GetCurrentTrack().trackMetadata.artist;
        genre.text = Navigation.GetCurrentTrack().trackMetadata.genre;
    }
}
