using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// TODO: deprecate this.
public class ResourcePanel : MonoBehaviour
{
    public static event UnityAction resourceRefreshed;

    public static List<string> GetAudioFiles()
    {
        return null;
    }
    public static List<string> GetImageFiles()
    {
        return null;
    }
    public static List<string> GetVideoFiles()
    {
        return null;
    }
}
