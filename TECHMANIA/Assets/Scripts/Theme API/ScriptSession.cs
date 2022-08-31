using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;
using UnityEngine.UIElements;
using System;

namespace ThemeApi
{
    public class ScriptSession
    {
        public static Script session { get; private set; }

        public static void Prepare()
        {
            // ==================== IMPORTANT =======================
            // IL2CPP builder may strip methods not called by any C#
            // code, causing them to be unavailable to Lua. To prevent
            // that, make sure to include in Assets/link.xml all Unity
            // and .Net classes exposed to Lua.
            // ==================== IMPORTANT =======================

            // Set up sandbox
            session = new Script(CoreModules.Preset_SoftSandbox);
            // Redirect print
            session.Options.DebugPrint = (s) => { Debug.Log(s); };
            // Register types
            UserData.RegisterType<VisualTreeAsset>();
            UserData.RegisterType<VisualElement>();
            UserData.RegisterType<PanelSettings>();
            UserData.RegisterType<UQueryState<VisualElement>>();
            UserData.RegisterType<IStyle>();
            UserData.RegisterType<ITransform>();
            UserData.RegisterType<StyleSheet>();
            UserData.RegisterType<StyleLength>();
            UserData.RegisterType<StyleTranslate>();
            UserData.RegisterType<StyleFloat>();
            UserData.RegisterType<AudioSource>();
            foreach (VisualElementWrap.EventType typeEnum in
                Enum.GetValues(typeof(VisualElementWrap.EventType)))
            {
                try
                {
                    Type type = VisualElementWrap.EventTypeEnumToType(
                        typeEnum);
                    UserData.RegisterType(type);
                }
                catch (Exception)
                {
                    // Skip unsupported event types
                    continue;
                }
            }
            UserData.RegisterType<bool>();
            UserData.RegisterType<int>();
            UserData.RegisterType<float>();
            UserData.RegisterType<Time>();
            UserData.RegisterType<Screen>();
            UserData.RegisterType<Resolution>();
            UserData.RegisterType<Mathf>();
            UserData.RegisterType<Vector3>();
            UserData.RegisterType<Texture2D>();
            // For Pattern.notes
            UserData.RegisterType<SortedSet<Note>>();
            UserData.RegisterAssembly();
            // Preparations
            Techmania.Prepare();
            // Expose API
            session.Globals["getApi"] = (Func<int, object>)GetApi;
            // Expose .Net & Unity classes
            session.Globals["netString"] = UserData.CreateStatic<StringWrap>();
            session.Globals["bool"] = UserData.CreateStatic<bool>();
            session.Globals["int"] = UserData.CreateStatic<int>();
            session.Globals["float"] = UserData.CreateStatic<float>();
            session.Globals["time"] = UserData.CreateStatic<Time>();
            session.Globals["screen"] = UserData.CreateStatic
                <Screen>();
            session.Globals["resolution"] = 
                UserData.CreateStatic<Resolution>();
            session.Globals["mathf"] = UserData.CreateStatic<Mathf>();
            session.Globals["vector3"] =
                UserData.CreateStatic<Vector3>();
        }

        public static void Execute(string script)
        {
            session.DoString(script);
        }

        public static object GetApi(int version)
        {
            switch (version)
            {
                case 1:
                    return new Techmania();
                default:
                    throw new ApiNotSupportedException();
            }
        }
    }

    public class ApiNotSupportedException : Exception { }
}