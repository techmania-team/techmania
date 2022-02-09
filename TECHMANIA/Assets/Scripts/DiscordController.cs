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
    Game
}

public class DiscordController
{
    private static Discord.Discord discord;
    public static Int64 timeStart;
    private static string details = "";
    private static string state = "";
    private static DiscordActivityType lastActivityType;

    public static void Start ()
    {
        if (discord != null || !SupportedOnCurrentPlatform()) return;
        discord = new Discord.Discord(802017593086836767, (UInt64)Discord.CreateFlags.Default);
    }
    
    public static void RunCallbacks ()
    {
        if (discord == null || !SupportedOnCurrentPlatform()) return;
        discord.RunCallbacks();
    }

    public static void SetActivity (DiscordActivityType type)
    {
        if (discord == null || !SupportedOnCurrentPlatform()) return;
        bool shouldSetTimestamp = false;
        switch (type)
        {
            case DiscordActivityType.MainMenu:
                details = "Main Menu";
                state = "";
                break;
            case DiscordActivityType.Options:
                details = "Options";
                state = "";
                break;
            case DiscordActivityType.Information:
                details = "Information";
                state = "";
                break;
            case DiscordActivityType.SelectingTrack:
                details = "Selecting Track";
                state = "";
                break;
            case DiscordActivityType.SelectingPattern:
                details = GameSetup.track.trackMetadata.title;
                state = "Selecting Pattern";
                break;
            case DiscordActivityType.EditorTrack:
                if (lastActivityType != DiscordActivityType.EditorTrack) timeStart = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                details = EditorContext.track.trackMetadata.title;
                state = "Editing Track";
                shouldSetTimestamp = true;
                break;
            case DiscordActivityType.EditorPattern:
                if (lastActivityType != DiscordActivityType.EditorPattern) timeStart = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                state = String.Format("Editing {0}L {1} - {2}", EditorContext.Pattern.patternMetadata.playableLanes, GetModeName(EditorContext.Pattern.patternMetadata.controlScheme), EditorContext.Pattern.patternMetadata.patternName);
                shouldSetTimestamp = true;
                break;
            case DiscordActivityType.EditorSave:
                details = EditorContext.track.trackMetadata.title;
                shouldSetTimestamp = true;
                break;
            case DiscordActivityType.Game:
                if (lastActivityType != DiscordActivityType.Game) timeStart = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                details = GameSetup.track.trackMetadata.title;
                state = String.Format("{0}L {1} - {2}", GameSetup.pattern.patternMetadata.playableLanes, GetModeName(GameSetup.pattern.patternMetadata.controlScheme), GameSetup.pattern.patternMetadata.patternName);
                shouldSetTimestamp = true;
                break;
        }
        lastActivityType = type;
        Discord.Activity activity = new Discord.Activity
        {
            Details = details,
            State = state,
            Assets = {
                LargeImage = "techmania"
            }
        };
        if (shouldSetTimestamp)
        {
            activity.Timestamps.Start = DiscordController.timeStart;
        }
        discord.GetActivityManager().UpdateActivity(activity, (res) => {});
    }

    public static void Dispose ()
    {
        if (discord == null || !SupportedOnCurrentPlatform()) return;
        discord.Dispose();
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
                return "Touch";
            case ControlScheme.Keys:
                return "Keys";
            case ControlScheme.KM:
                return "KM";
            default:
                return "";
        }
    }
}
