using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;
using UnityEngine.UIElements;
using System;

namespace ThemeApi
{
    public static class ScriptSession
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
            UserData.RegisterType<AudioClip>();
            UserData.RegisterType<Texture2D>();
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
            UserData.RegisterType<Rect>();
            UserData.RegisterType<KeyCode>();
            UserData.RegisterAssembly();

            // Expose API
            session.Globals["getApi"] = (Func<int, object>)GetApi;

            // Expose .Net classes
            Table netTypes = new Table(session);
            UserData.RegisterType<bool>();
            netTypes["bool"] = UserData.CreateStatic<bool>();
            UserData.RegisterType<int>();
            netTypes["int"] = UserData.CreateStatic<int>();
            UserData.RegisterType<float>();
            netTypes["float"] = UserData.CreateStatic<float>();
            netTypes["string"] = UserData.CreateStatic<StringWrap>();
            session.Globals["net"] = netTypes;

            // Expose Unity classes
            Table unityTypes = new Table(session);
            UserData.RegisterType<Time>();
            unityTypes["time"] = UserData.CreateStatic<Time>();
            UserData.RegisterType<Screen>();
            unityTypes["screen"] = UserData.CreateStatic
                <Screen>();
            UserData.RegisterType<Resolution>();
            unityTypes["resolution"] =
                UserData.CreateStatic<Resolution>();
            UserData.RegisterType<Mathf>();
            unityTypes["mathf"] = UserData.CreateStatic<Mathf>();
            UserData.RegisterType<Vector3>();
            unityTypes["vector3"] =
                UserData.CreateStatic<Vector3>();
            session.Globals["unity"] = unityTypes;

            Table typeTable = new Table(session);
            typeTable["KeyCode"] = UserData.CreateStatic<KeyCode>();
            session.Globals["enums"] = typeTable;
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