using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TrackCard : MonoBehaviour
{
    public EyecatchSelfLoader eyecatch;
    public TextMeshProUGUI title;
    public TextMeshProUGUI artist;

    public void Initialize(string folder, TrackMetadata t)
    {
        eyecatch.LoadImage(folder, t);
        title.text = t.title;
        artist.text = t.artist;
    }
}
