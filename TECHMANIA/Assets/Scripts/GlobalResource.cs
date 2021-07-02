using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GlobalResource
{
    public static NoteSkin noteSkin;
    public static VfxSkin vfxSkin;
    public static ComboSkin comboSkin;
    public static GameUISkin gameUiSkin;
    public static bool loaded;

    static GlobalResource()
    {
        loaded = false;
    }
}
