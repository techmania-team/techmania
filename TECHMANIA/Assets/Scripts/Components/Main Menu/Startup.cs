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
        SpriteSheet.PrepareEmptySpriteSheet();
        Records.RefreshInstance();
#if UNITY_ANDROID
        AndroidUtility.CheckVersion();
        // Ask for storage permission before loading resource if custom data location is set
        if (Options.instance.customDataLocation)
        {
            StartCoroutine(AndroidUtility.AskForPermissions(OnAndroidPermissionAsked));
        }
        else
        {
            // This prevents loading custom skins from streaming assets at startup.
            // Android Play Games may sync your options after reinstall the game.
            // So we have to reset skins if custom data location is not set but using custom skins.
            Options.ResetCustomDataLocation();
            LoadResources();
        }
#else
        LoadResources();
#endif
        DiscordController.Start();
        DiscordController.SetActivity(DiscordActivityType.MainMenu);
    }

    void LoadResources ()
    {
        Paths.ApplyCustomDataLocation();
        BetterStreamingAssets.Initialize();
        StartCoroutine(GetComponent<GlobalResourceLoader>().LoadResources(reload: false, finishCallback: null));
    }

#if UNITY_ANDROID
    void OnAndroidPermissionAsked ()
    {
        // Turn off custom data location and reset skins if user denied permission.
        // Otherwise. there will be an error while loading skins.
        if (!AndroidUtility.HasStoragePermissions())
        {
            Options.ResetCustomDataLocation();
        }
        LoadResources();
    }
#endif
}
