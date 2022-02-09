using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Startup : MonoBehaviour
{
    public TextAsset stringTable;
    public AudioSourceManager audioSourceManager;
    public LoadScreen loadScreen;

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
        Options.instance.ApplyGraphicSettings();
        Options.instance.ApplyAudioBufferSize();
        audioSourceManager.ApplyVolume();
        LoadRuleset();
        Paths.ApplyCustomDataLocation();

        Locale.Initialize(stringTable);
        Locale.SetLocale(Options.instance.locale);

        SpriteSheet.PrepareEmptySpriteSheet();
        Records.RefreshInstance();
        BetterStreamingAssets.Initialize();

        // GetComponent<GlobalResourceLoader>().StartLoading();
        loadScreen.StartLoading();
    }
}
