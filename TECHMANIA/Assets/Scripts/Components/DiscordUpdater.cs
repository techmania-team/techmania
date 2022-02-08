using UnityEngine;

public class DiscordUpdater : MonoBehaviour
{
#if !UNITY_IOS && !UNITY_ANDROID
    void Update ()
    {
        DiscordController.RunCallbacks();
    }
    
    void OnApplicationQuit()
    {
        DiscordController.Dispose();
    }
#endif
}
