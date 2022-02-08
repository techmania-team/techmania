using System;

public class DiscordController
{
    private static Discord.Discord discord;

    public static Int64 timeStart;

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

    public static void SetActivity (Discord.Activity activity)
    {
        if (discord == null || !SupportedOnCurrentPlatform()) return;
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

    public static string GetModeName (ControlScheme controlScheme)
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
