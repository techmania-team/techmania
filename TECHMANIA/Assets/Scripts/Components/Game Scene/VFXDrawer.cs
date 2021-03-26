using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// This would be called VFXRenderer, but apparently
// "Script 'VFXRenderer' has the same name as built-in Unity component."
//
// What the heck.
public class VFXDrawer : MonoBehaviour
{
    private SpriteSheetForVfx spriteSheet;
    private bool loop;

    private RectTransform rect;
    private Image image;
    private float startTime;

    public void Initialize(Vector3 position,
        SpriteSheetForVfx spriteSheet, bool loop)
    {
        transform.position = position;
        this.spriteSheet = spriteSheet;
        this.loop = loop;
    }

    // Start is called before the first frame update
    void Start()
    {
        rect = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        startTime = Game.Time;

        if (spriteSheet.sprites == null ||
            spriteSheet.sprites.Count == 0)
        {
            Destroy(gameObject);
            return;
        }

        image.sprite = spriteSheet.sprites[0];
        float height = Scan.laneHeight * spriteSheet.scale;
        float width = spriteSheet.sprites[0].rect.width /
            spriteSheet.sprites[0].rect.height * height;
        rect.sizeDelta = new Vector2(width, height);
    }

    // Update is called once per frame
    void Update()
    {
        float time = Game.Time - startTime;
        Sprite sprite = spriteSheet.GetSpriteForTime(time, loop);
        if (sprite == null)
        {
            Destroy(gameObject);
        }
        else
        {
            image.sprite = sprite;
        }
    }
}
