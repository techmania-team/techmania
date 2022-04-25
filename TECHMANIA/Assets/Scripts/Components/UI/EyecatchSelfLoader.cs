using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
            string fullPath = UniversalIO.PathCombine(folder, t.eyecatchImage);
            ResourceLoader.LoadImage(fullPath, OnLoadImageComplete);
        }
        else
        {
            NoImage();
        }
    }

    public void LoadImage(string fullPath)
    {
        if (fullPath != null && fullPath != "")
        {
            ResourceLoader.LoadImage(fullPath, OnLoadImageComplete);
        }
        else
        {
            NoImage();
        }
    }

    private void OnLoadImageComplete(Texture2D texture, string error)
    {
        if (!gameObject.activeInHierarchy) return;
        if (texture == null)
        {
            NoImage();
            return;
        }

        image.sprite = ResourceLoader.CreateSpriteFromTexture(
            texture);
        image.gameObject.SetActive(true);
        progressIndicator.SetActive(false);
        noImageIndicator.SetActive(false);
    }

    private void OnDisable()
    {
        if (image.sprite != null)
        {
            Destroy(image.sprite.texture);
        }
    }
}
