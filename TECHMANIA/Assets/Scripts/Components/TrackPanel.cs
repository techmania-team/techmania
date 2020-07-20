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

    private void UpdateProperty(ref string property, string newValue, ref bool madeRecord)
    {
        if (property == newValue)
        {
            return;
        }
        if (!madeRecord)
        {
            Navigation.PrepareForChange();
            madeRecord = true;
        }
        property = newValue;
    }

    public void UIToMemory()
    {
        bool madeChange = false;
        UpdateProperty(ref Navigation.GetCurrentTrack().trackMetadata.title,
            title.text, ref madeChange);
        UpdateProperty(ref Navigation.GetCurrentTrack().trackMetadata.artist,
            artist.text, ref madeChange);
        UpdateProperty(ref Navigation.GetCurrentTrack().trackMetadata.genre,
            genre.text, ref madeChange);

        if (madeChange)
        {
            Navigation.DoneWithChange();
        }
    }

    public void MemoryToUI()
    {
        // This does NOT fire EndEdit events.
        title.text = Navigation.GetCurrentTrack().trackMetadata.title;
        artist.text = Navigation.GetCurrentTrack().trackMetadata.artist;
        genre.text = Navigation.GetCurrentTrack().trackMetadata.genre;
    }
}
