using System;

public enum DiscordActivityType
{
    MainMenu,
    Options,
    Information,
    SelectingTrack,
    SelectingPattern,
    EditorTrack,
    EditorPattern,
    EditorSave,
    Game,
    Empty
}

public class DiscordController
{
    private static Discord.Discord discord;
    private static DateTimeOffset timeStart;
    private static string details = "";
    private static string state = "";
    private static DiscordActivityType lastActivityType;

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

    public static void SetActivity (DiscordActivityType type)
    {
        if (discord == null || !SupportedOnCurrentPlatform()) return;

        bool shouldSetTimestamp = false;
        switch (type)
        {
            case DiscordActivityType.MainMenu:
                details = L10n.GetString("discord_state_main_menu");
                state = "";
                break;
            case DiscordActivityType.Options:
                details = L10n.GetString("discord_state_options");
                state = "";
                break;
            case DiscordActivityType.Information:
                details = L10n.GetString("discord_state_information");
                state = "";
                break;
            case DiscordActivityType.SelectingTrack:
                details = L10n.GetString(
                    "discord_state_selecting_track");
                state = "";
                break;
            case DiscordActivityType.SelectingPattern:
                details = InternalGameSetup.track.trackMetadata.title;
                state = L10n.GetString(
                    "discord_state_selecting_pattern");
                break;
            case DiscordActivityType.EditorTrack:
                if (lastActivityType
                    != DiscordActivityType.EditorTrack)
                {
                    timeStart = DateTimeOffset.UtcNow;
                }
                details = EditorContext.track.trackMetadata.title;
                state = L10n.GetString("discord_state_editing_track");
                shouldSetTimestamp = true;
                break;
            case DiscordActivityType.EditorPattern:
                if (lastActivityType !=
                    DiscordActivityType.EditorPattern)
                {
                    timeStart = DateTimeOffset.UtcNow;
                }
                {
                    PatternMetadata metadata = EditorContext.Pattern
                        .patternMetadata;
                    state = L10n.GetStringAndFormat(
                        "discord_state_editing_pattern",
                        metadata.playableLanes,
                        GetModeName(metadata.controlScheme),
                        metadata.patternName);
                }
                shouldSetTimestamp = true;
                break;
            case DiscordActivityType.EditorSave:
                details = EditorContext.track.trackMetadata.title;
                shouldSetTimestamp = true;
                break;
            case DiscordActivityType.Game:
                if (lastActivityType != DiscordActivityType.Game)
                {
                    timeStart = DateTimeOffset.UtcNow;
                }
                {
                    PatternMetadata metadata = InternalGameSetup.patternAfterModifier
                        .patternMetadata;
                    details = InternalGameSetup.track.trackMetadata.title;
                    state = L10n.GetStringAndFormat(
                        "discord_state_playing_pattern",
                        metadata.playableLanes,
                        GetModeName(metadata.controlScheme),
                        metadata.patternName);
                }
                shouldSetTimestamp = true;
                break;
        }
        lastActivityType = type;
        Discord.Activity activity = new Discord.Activity
        {
            Details = details,
            State = state,
            Assets =
            {
                LargeImage = "techmania"
            }
        };
        if (shouldSetTimestamp)
        {
            activity.Timestamps.Start = timeStart.ToUnixTimeSeconds();
        }
        discord.GetActivityManager().UpdateActivity(
            activity, (result) => {});
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
    private static bool showingElapsedTime = false;

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
                timeStart = DateTimeOffset.UtcNow;
            }
            activity.Timestamps.Start = timeStart.ToUnixTimeSeconds();
        }
        discord.GetActivityManager().UpdateActivity(
            activity, (result) => { });
        showingElapsedTime = showElapsedTime;
    }
    #endregion
}
