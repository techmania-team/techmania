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
        Texture2D texture = DownloadHandlerTexture.GetContent(request);
        Debug.Log($"Loaded: {filename}");

        List<Sprite> sprites = new List<Sprite>();
        int spriteWidth = texture.width / noteSkin.basic.columns;
        int spriteHeight = texture.height / noteSkin.basic.rows;
        for (int i = noteSkin.basic.firstIndex;
            i <= noteSkin.basic.lastIndex; i++)
        {
            int row = i / noteSkin.basic.columns;
            // (0, 0) is bottom left, so we inverse y here.
            int inverseRow = noteSkin.basic.rows - 1 - row;
            int column = i % noteSkin.basic.columns;
            Sprite s = Sprite.Create(texture,
                new Rect(column * spriteWidth,
                inverseRow * spriteHeight,
                spriteWidth, spriteHeight),
                new Vector2(0.5f, 0.5f));
            sprites.Add(s);
        }

        int frameNumber = 0;
        while (true)
        {
            image.sprite = sprites[frameNumber % sprites.Count];
            frameNumber++;

            yield return new WaitForSeconds(0.03f);
        }
    }
}
