using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class EyecatchSelfLoader : MonoBehaviour
{
    public Image image;
    public GameObject progressIndicator;
    public GameObject noImageIndicator;

    private void NoImage()
    {
        image.gameObject.SetActive(false);
        progressIndicator.SetActive(false);
        noImageIndicator.SetActive(true);
    }

    public void LoadImage(string folder, TrackMetadata t)
    {
        if (t.eyecatchImage != null &&
            t.eyecatchImage != "")
        {
            string fullPath = folder + "\\" + t.eyecatchImage;
            ResourceLoader.LoadImage(fullPath, OnLoadImageComplete);
        }
        else
        {
            NoImage();
        }
    }

    private void OnLoadImageComplete(Sprite sprite, string error)
    {
        if (!gameObject.activeInHierarchy) return;
        if (sprite == null)
        {
            NoImage();
            return;
        }

        image.sprite = sprite;
        image.gameObject.SetActive(true);
        progressIndicator.SetActive(false);
        noImageIndicator.SetActive(false);
    }
}
