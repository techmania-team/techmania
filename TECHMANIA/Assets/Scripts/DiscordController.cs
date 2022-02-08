using System;

public class DiscordController
{
    private static Discord.Discord discord;
    public static Int64 timeStart;
    private static string details = "";
    private static string state = "";
    private static string lastActivityType = "";

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

    public static void SetActivity (string type)
    {
        if (discord == null || !SupportedOnCurrentPlatform()) return;
        bool timestamp = false;
        switch (type)
        {
            case "Main Menu":
                details = "Main Menu";
                state = "";
                break;
            case "Options":
                details = "Options";
                state = "";
                break;
            case "Information":
                details = "Information";
                state = "";
                break;
            case "Selecting Track":
                details = "Selecting Track";
                state = "";
                break;
            case "Selecting Pattern":
                details = GameSetup.track.trackMetadata.title;
                state = "Selecting Pattern";
                break;
            case "Editor Track":
                if (lastActivityType != "Editing Track") timeStart = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                details = EditorContext.track.trackMetadata.title;
                state = "Editing Track";
                timestamp = true;
                break;
            case "Editor Pattern":
                if (lastActivityType != "Editor Pattern") timeStart = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                state = String.Format("Editing {0}L {1} - {2}", EditorContext.Pattern.patternMetadata.playableLanes, GetModeName(EditorContext.Pattern.patternMetadata.controlScheme), EditorContext.Pattern.patternMetadata.patternName);
                timestamp = true;
                break;
            case "Editor Save":
                details = EditorContext.track.trackMetadata.title;
                timestamp = true;
                break;
            case "Game":
                if (lastActivityType != "Game") timeStart = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                details = GameSetup.track.trackMetadata.title;
                state = String.Format("{0}L {1} - {2}", GameSetup.pattern.patternMetadata.playableLanes, GetModeName(GameSetup.pattern.patternMetadata.controlScheme), GameSetup.pattern.patternMetadata.patternName);
                timestamp = true;
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
        if (timestamp)
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
