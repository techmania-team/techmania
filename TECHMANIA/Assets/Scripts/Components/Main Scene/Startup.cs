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
        if (Options.instance.rulesetEnum != Options.Ruleset.Custom)
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
            Options.instance.rulesetEnum = Options.Ruleset.Standard;
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

        L10n.Initialize(stringTable.text, L10n.Instance.System);
        L10n.SetLocale(Options.instance.locale, L10n.Instance.System);

        SpriteSheet.PrepareEmptySpriteSheet();
        Records.RefreshInstance();
        BetterStreamingAssets.Initialize();

        loadScreen.StartLoading();
    }
}
