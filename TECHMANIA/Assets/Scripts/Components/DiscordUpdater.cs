using UnityEngine;

public class DiscordUpdater : MonoBehaviour
{
    void Update ()
    {
        DiscordController.RunCallbacks();
    }
    
    void OnApplicationQuit()
    {
        DiscordController.Dispose();
    }
}
