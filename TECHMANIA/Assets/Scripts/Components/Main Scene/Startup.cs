using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Startup : MonoBehaviour
{
    public TextAsset stringTable;
    public AudioManager audioManager;
    public BootScreen bootScreen;

    public AnimationCurve curve;

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
        
        return;

        Input.simulateMouseWithTouches = false;
        Paths.PrepareFolders();
        Options.RefreshInstance();
        Statistics.RefreshInstance();
        Statistics.instance.timesAppLaunched++;
        GetComponent<StatsMaintainer>().BeginWorking();

        FmodManager.instance.Initialize(
            Options.instance.audioBufferSize,
            Options.instance.numAudioBuffers);

        Options.instance.SetDefaultResolutionIfInvalid();
        Options.instance.ApplyGraphicSettings();
        audioManager.ApplyVolume();
        Options.instance.ApplyAsio();
        LoadRuleset();
        
        L10n.Initialize(stringTable.text, L10n.Instance.System);
        L10n.SetLocale(Options.instance.locale, L10n.Instance.System);

        SpriteSheet.PrepareEmptySpriteSheet();
        Records.RefreshInstance();

        DiscordController.Start();

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
            StartBooting();
        }
#else
        StartBooting();
#endif
    }

    private void OnGUI()
    {
        StringBuilder stringBuilder = new StringBuilder();
        foreach (Keyframe k in curve.keys)
        {
            stringBuilder.AppendLine($"time: {k.time}");
            stringBuilder.AppendLine($"value: {k.value}");
            stringBuilder.AppendLine($"inTangent: {k.inTangent}");
            stringBuilder.AppendLine($"outTangent: {k.outTangent}");
            stringBuilder.AppendLine($"inWeight: {k.inWeight}");
            stringBuilder.AppendLine($"outWeight: {k.outWeight}");
            stringBuilder.AppendLine($"weightedMode: {k.weightedMode}");
            stringBuilder.AppendLine();
        }
        GUI.Window(0, new Rect(10, 10, 500, 500), (int windowID) =>
        {
            GUI.TextArea(new Rect(0, 20, 500, 500), stringBuilder.ToString());
        }, "Curve");
    }

    private void StartBooting()
    {
        Paths.ApplyCustomDataLocation();
        BetterStreamingAssets.Initialize();
        bootScreen.StartBooting();
    }
}
