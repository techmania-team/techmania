using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SpriteSheet
{
    public string filename;
    public int rows;
    public int columns;
    public int firstIndex;
    public int lastIndex;

    [NonSerialized]  // Loaded at runtime
    public Texture2D texture;
    [NonSerialized]
    public List<Sprite> sprites;

    public SpriteSheet()
    {
        rows = 1;
        columns = 1;
        firstIndex = 0;
        lastIndex = 0;
    }

    // Call after loading texture.
    public void GenerateSprites()
    {
        if (texture == null)
        {
            throw new Exception("Texture not yet loaded.");
        }
        sprites = new List<Sprite>();
        int spriteWidth = texture.width / columns;
        int spriteHeight = texture.height / rows;
        for (int i = firstIndex; i <= lastIndex; i++)
        {
            int row = i / columns;
            // Unity thinks (0, 0) is bottom left but we think
            // (0, 0) is top left. So we inverse y here.
            int inverseRow = rows - 1 - row;
            int column = i % columns;
            Sprite s = Sprite.Create(texture,
                new Rect(column * spriteWidth,
                    inverseRow * spriteHeight,
                    spriteWidth,
                    spriteHeight),
                new Vector2(0.5f, 0.5f));
            sprites.Add(s);
        }
    }
}

[Serializable]
public class SpriteSheetForNote : SpriteSheet
{
    public float scale;  // Relative to 1x lane height

    public SpriteSheetForNote() : base()
    {
        scale = 1f;
    }
}

[Serializable]
public class SpriteSheetForVfx : SpriteSheet
{
    public float scale;  // Relative to 1x lane height
    public float speed;  // Relative to 60 fps

    public SpriteSheetForVfx() : base()
    {
        scale = 1f;
        speed = 1f;
    }
}

[Serializable]
[FormatVersion(NoteSkin.kVersion, typeof(NoteSkin), isLatest: true)]
public class NoteSkinBase : Serializable<NoteSkinBase> {}

public class NoteSkin : NoteSkinBase
{
    public const string kVersion = "1";

    // Note skin's name is the folder's name.

    public SpriteSheetForNote basic;

    public SpriteSheetForNote chainHead;
    public SpriteSheetForNote chainNode;
    public SpriteSheetForNote chainPath;

    public SpriteSheetForNote holdHead;
    public SpriteSheetForNote holdTrail;
    public SpriteSheetForNote holdTrailEnd;
    public SpriteSheetForNote holdOngoingTrail;
    public SpriteSheetForNote holdOngoingTrailEnd;

    public SpriteSheetForNote dragHead;
    public SpriteSheetForNote dragCurve;

    public SpriteSheetForNote repeatHead;
    public SpriteSheetForNote repeat;
    public SpriteSheetForNote repeatHoldTrail;
    public SpriteSheetForNote repeatHoldTrailEnd;
    public SpriteSheetForNote repeatPath;

    public NoteSkin()
    {
        version = kVersion;
    }

    public List<SpriteSheetForNote> GetReferenceToAllSpriteSheets()
    {
        List<SpriteSheetForNote> list = new List<SpriteSheetForNote>();

        list.Add(basic);

        list.Add(chainHead);
        list.Add(chainNode);
        list.Add(chainPath);

        list.Add(holdHead);
        list.Add(holdTrail);
        list.Add(holdTrailEnd);
        list.Add(holdOngoingTrail);
        list.Add(holdOngoingTrailEnd);

        list.Add(dragHead);
        list.Add(dragCurve);

        list.Add(repeatHead);
        list.Add(repeat);
        list.Add(repeatHoldTrail);
        list.Add(repeatHoldTrailEnd);
        list.Add(repeatPath);

        return list;
    }
}