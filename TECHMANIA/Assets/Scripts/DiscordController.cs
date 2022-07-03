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
        catch {}
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
                details = Locale.GetString("discord_state_main_menu");
                state = "";
                break;
            case DiscordActivityType.Options:
                details = Locale.GetString("discord_state_options");
                state = "";
                break;
            case DiscordActivityType.Information:
                details = Locale.GetString("discord_state_information");
                state = "";
                break;
            case DiscordActivityType.SelectingTrack:
                details = Locale.GetString(
                    "discord_state_selecting_track");
                state = "";
                break;
            case DiscordActivityType.SelectingPattern:
                details = GameSetup.track.trackMetadata.title;
                state = Locale.GetString(
                    "discord_state_selecting_pattern");
                break;
            case DiscordActivityType.EditorTrack:
                if (lastActivityType
                    != DiscordActivityType.EditorTrack)
                {
                    timeStart = DateTimeOffset.UtcNow;
                }
                details = EditorContext.track.trackMetadata.title;
                state = Locale.GetString("discord_state_editing_track");
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
                    state = Locale.GetStringAndFormat(
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
                    PatternMetadata metadata = GameSetup.pattern
                        .patternMetadata;
                    details = GameSetup.track.trackMetadata.title;
                    state = Locale.GetStringAndFormat(
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
                return Locale.GetString(
                    "track_setup_patterns_tab_control_scheme_touch");
            case ControlScheme.Keys:
                return Locale.GetString(
                    "track_setup_patterns_tab_control_scheme_keys");
            case ControlScheme.KM:
                return Locale.GetString(
                    "track_setup_patterns_tab_control_scheme_km");
            default:
                return "";
        }
    }
}
