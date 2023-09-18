using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StopPreviewTrackOnDisable : MonoBehaviour
{
    public PreviewTrackPlayer player;

    private void OnDisable()
    {
        player.Stop();
    }
}
