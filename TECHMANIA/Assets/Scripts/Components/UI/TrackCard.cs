using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TrackCard : MonoBehaviour
{
    public EyecatchSelfLoader eyecatch;
    public TextMeshProUGUI title;
    public TextMeshProUGUI artist;

    public void Initialize(string eyecatchPath,
        string title, string artist)
    {
        if (eyecatchPath == null || eyecatchPath.Length == 0)
        {
            eyecatch.NoImage();
        }
        else
        {
            eyecatch.LoadImage(eyecatchPath);
        }
        this.title.text = title;
        this.artist.text = artist;
    }
}
