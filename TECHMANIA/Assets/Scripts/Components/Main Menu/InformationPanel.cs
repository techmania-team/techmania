using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InformationPanel : MonoBehaviour
{
    public TextMeshProUGUI tracksSkinsLocation;

    // Start is called before the first frame update
    private void OnEnable()
    {
#if UNITY_IOS
        tracksSkinsLocation.text = Locale.GetString(
            "information_panel_tracks_skins_location_ios");
#elif UNITY_ANDROID
        tracksSkinsLocation.text = Locale.GetString(
            "information_panel_tracks_skins_location_android");
#else
        tracksSkinsLocation.text = Locale.GetString(
            "information_panel_tracks_skins_location_pc");
#endif
        DiscordController.SetActivity(DiscordActivityType.Information);
    }

    public void OnWebsiteButtonClick()
    {
        Application.OpenURL("https://techmania-team.herokuapp.com/");
    }

    public void OnDiscordButtonClick()
    {
        Application.OpenURL("https://discord.gg/K4Nf7AnAZt");
    }

    public void OnGitHubButtonClick()
    {
        Application.OpenURL(
            "https://github.com/techmania-team/techmania");
    }

    public void OnDocumentationButtonClick()
    {
        Application.OpenURL(
            "https://techmania-team.github.io/techmania-docs/");
    }

    public void OnYouTubeButtonClick()
    {
        Application.OpenURL(
            "https://www.youtube.com/channel/UCoHxk7shdAKf7W3yqUJlDaA");
    }
}
