using System;
using UnityEngine;

public class DiscordController : MonoBehaviour
{
#if !UNITY_IOS && !UNITY_ANDROID
    private static Discord.Discord discord;

    public static void Start ()
    {
        if (discord != null) return;
        discord = new Discord.Discord(802017593086836767, (System.UInt64)Discord.CreateFlags.Default);
    }
    
    public static void RunCallbacks ()
    {
        if (discord == null) return;
        discord.RunCallbacks();
    }

    public static void SetActivity (Discord.Activity activity)
    {
        if (discord == null) return;
        discord.GetActivityManager().UpdateActivity(activity, (res) => {});
    }

    public static void Dispose ()
    {
        if (discord == null) return;
        discord.Dispose();
    }
#endif
}
