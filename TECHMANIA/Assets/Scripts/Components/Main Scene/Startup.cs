﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Startup : MonoBehaviour
{
    public TextAsset stringTable;
    public AudioSourceManager audioSourceManager;
    public BootScreen bootScreen;

    private static void LoadRuleset()
    {
        if (Options.instance.ruleset != Options.Ruleset.Custom)
        {
            return;
        }

        try
        {
            Ruleset.LoadCustomRuleset();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("An error occurred when loading custom ruleset, reverting to standard ruleset: " + ex.ToString());
            // Silently ignore errors.
            Options.instance.ruleset = Options.Ruleset.Standard;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        Input.simulateMouseWithTouches = false;
        Paths.PrepareFolders();

        Options.RefreshInstance();
        Options.instance.SetDefaultResolutionIfInvalid();
        Options.instance.ApplyGraphicSettings();
        Options.instance.ApplyAudioBufferSize();
        audioSourceManager.ApplyVolume();
        LoadRuleset();
        
        L10n.Initialize(stringTable.text, L10n.Instance.System);
        L10n.SetLocale(Options.instance.locale, L10n.Instance.System);

        SpriteSheet.PrepareEmptySpriteSheet();
        Records.RefreshInstance();

        DiscordController.Start();
        DiscordController.SetActivity(DiscordActivityType.MainMenu);

#if UNITY_ANDROID
        AndroidUtility.CheckVersion();
        // Ask for storage permission before loading resource
        // if custom data location is set.
        if (Options.instance.customDataLocation)
        {
            StartCoroutine(AndroidUtility.AskForPermissions(
                callback: () =>
                {
                    // Turn off custom data location and reset skins
                    // if user denied permission.
                    // Otherwise, there will be an error while loading skins.
                    if (!AndroidUtility.HasStoragePermissions())
                    {
                        Options.instance.ResetCustomDataLocation();
                    }
                    StartBooting();
                }));
        }
        else
        {
            // This prevents loading custom skins from streaming assets
            // at startup. Android Play Games may sync your options after
            // reinstall the game So we have to reset skins if custom
            // data location is not set but using custom skins.
            Options.instance.ResetCustomDataLocation();
            StartBooting();
        }
#else
        StartBooting();
#endif
    }

    private void StartBooting()
    {
        Paths.ApplyCustomDataLocation();
        BetterStreamingAssets.Initialize();
        bootScreen.StartBooting();
    }
}
