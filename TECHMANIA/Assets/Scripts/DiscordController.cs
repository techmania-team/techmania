using System;

public enum DiscordActivityType
{
    EditorTrack,
    EditorPattern,
    EditorSetlist
}

public class DiscordController
{
    private static Discord.Discord discord;
    private static bool showingElapsedTime = false;
    private static DateTimeOffset startTime;

    public static void Start ()
    {
        if (discord != null ||
            !SupportedOnCurrentPlatform() ||
            !Options.instance.discordRichPresence) return;
        try
        {
            discord = new Discord.Discord(802017593086836767,
                (UInt64)Discord.CreateFlags.NoRequireDiscord);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogException(ex);
        }
    }
    
    public static void RunCallbacks ()
    {
        if (discord == null || !SupportedOnCurrentPlatform()) return;
        try
        {
            discord.RunCallbacks();
        }
        catch
        {
            Dispose();
        }
    }

    // For editor only.
    public static void SetActivity (DiscordActivityType type)
    {
        if (discord == null || !SupportedOnCurrentPlatform()) return;

        switch (type)
        {
            case DiscordActivityType.EditorTrack:
                SetActivity(
                    details: 
                    EditorContext.track.trackMetadata.title,
                    state:
                    L10n.GetString("discord_state_editing_track"),
                    showElapsedTime: true);
                break;
            case DiscordActivityType.EditorPattern:
                PatternMetadata metadata = EditorContext.Pattern
                    .patternMetadata;
                string state = L10n.GetStringAndFormat(
                    "discord_state_editing_pattern",
                    metadata.playableLanes,
                    GetModeName(metadata.controlScheme),
                    metadata.patternName);
                SetActivity(
                    details:
                    EditorContext.track.trackMetadata.title,
                    state: state,
                    showElapsedTime: true);
                break;
            case DiscordActivityType.EditorSetlist:
                SetActivity(
                    details: EditorContext.setlist.setlistMetadata.title,
                    state: L10n.GetString(
                        "discord_state_editing_setlist"),
                    showElapsedTime: true);
                break;
        }
    }

    public static void Dispose ()
    {
        if (discord == null || !SupportedOnCurrentPlatform()) return;
        discord.Dispose();
        discord = null;
    }

    public static bool SupportedOnCurrentPlatform ()
    {
#if !UNITY_IOS && !UNITY_ANDROID
        return true;
#else
        return false;
#endif
    }

    private static string GetModeName (ControlScheme controlScheme)
    {
        switch (controlScheme)
        {
            case ControlScheme.Touch:
                return L10n.GetString(
                    "track_setup_patterns_tab_control_scheme_touch");
            case ControlScheme.Keys:
                return L10n.GetString(
                    "track_setup_patterns_tab_control_scheme_keys");
            case ControlScheme.KM:
                return L10n.GetString(
                    "track_setup_patterns_tab_control_scheme_km");
            default:
                return "";
        }
    }

    #region Theme API
    
    public static void SetActivity(
        string details, string state, bool showElapsedTime = false)
    {
        if (discord == null || !SupportedOnCurrentPlatform()) return;

        Discord.Activity activity = new Discord.Activity
        {
            Details = details,
            State = state,
            Assets =
            {
                LargeImage = "techmania"
            }
        };
        if (showElapsedTime)
        {
            if (!showingElapsedTime)
            {
                // Capture start time.
                startTime = DateTimeOffset.UtcNow;
            }
            activity.Timestamps.Start = startTime.ToUnixTimeSeconds();
        }
        discord.GetActivityManager().UpdateActivity(
            activity, (result) => { });
        showingElapsedTime = showElapsedTime;
    }
    #endregion
}
