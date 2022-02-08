using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Startup : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Input.simulateMouseWithTouches = false;
        Paths.PrepareFolders();
        OptionsPanel.ApplyOptionsOnStartUp();
        Paths.ApplyCustomDataLocation();
        SpriteSheet.PrepareEmptySpriteSheet();
        Records.RefreshInstance();
        BetterStreamingAssets.Initialize();
        GetComponent<GlobalResourceLoader>().StartLoading();

#if !UNITY_IOS && !UNITY_ANDROID
        DiscordController.Start();
        DiscordController.SetActivity(new Discord.Activity
        {
            Details = "Main Menu",
            Assets = {
                LargeImage = "techmania"
            }
        });
#endif
    }
}
