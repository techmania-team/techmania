using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

public class TestScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(LoadSpriteSheet());
    }

    private IEnumerator LoadSpriteSheet()
    {
        Image image = GetComponent<Image>();

        string skinFolder = @"C:\Code\techmania\TECHMANIA\Builds\Skins\Note\Default";
        NoteSkin noteSkin = NoteSkin.LoadFromFile(
            Path.Combine(skinFolder, "skin.json")) as NoteSkin;
        string filename = Path.Combine(skinFolder, 
            noteSkin.basic.filename);
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(
            filename, nonReadable: true);
        yield return request.SendWebRequest();
        noteSkin.basic.texture = DownloadHandlerTexture.GetContent(request);
        Debug.Log($"Loaded: {filename}");
        noteSkin.basic.GenerateSprites();

        int frameNumber = 0;
        while (true)
        {
            image.sprite = noteSkin.basic.sprites[
                frameNumber % noteSkin.basic.sprites.Count];
            frameNumber++;

            yield return new WaitForSeconds(0.03f);
        }
    }
}
