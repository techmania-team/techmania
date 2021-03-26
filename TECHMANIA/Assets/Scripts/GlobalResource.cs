using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GlobalResource
{
    public static NoteSkin noteSkin;
    public static VfxSkin vfxSkin;
    public static bool loaded;
    // TODO: Combo skin

    static GlobalResource()
    {
        loaded = false;
    }
}
