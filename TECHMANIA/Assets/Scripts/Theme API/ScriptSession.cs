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
            UserData.RegisterAssembly();
            UserData.RegisterType<VisualTreeAsset>();
            UserData.RegisterType<VisualElement>();
            UserData.RegisterType<Painter2D>();
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
            UserData.RegisterType<Rect>();
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

            // Expose API
            session.Globals["getApi"] = (Func<int, Table>)GetApi;
        }

        public static void Execute(string script)
        {
            session.DoString(script);
        }

        public static Table GetApi(int version)
        {
            switch (version)
            {
                case 1:
                    return GetApiVersion1();
                default:
                    throw new ApiNotSupportedException();
            }
        }

        private static Table GetApiVersion1()
        {
            Table apiTable = new Table(session);

            // Expose Techmania class and enums
            Techmania tm = new Techmania();
            Table tmEnums = new Table(session);
            UserData.RegisterType<VisualElementWrap.EventType>();
            tmEnums["eventType"] = UserData.CreateStatic<
                VisualElementWrap.EventType>();
            UserData.RegisterType<Options.Ruleset>();
            tmEnums["ruleset"] = UserData.CreateStatic<
                Options.Ruleset>();
            UserData.RegisterType<AudioManager.Channel>();
            tmEnums["audioChannel"] = UserData.CreateStatic<
                AudioManager.Channel>();
            tm.@enum = tmEnums;
            apiTable["tm"] = UserData.Create(tm);

            // Expose .Net classes
            Table netTypes = new Table(session);
            UserData.RegisterType<bool>();
            netTypes["bool"] = UserData.CreateStatic<bool>();
            UserData.RegisterType<int>();
            netTypes["int"] = UserData.CreateStatic<int>();
            UserData.RegisterType<float>();
            netTypes["float"] = UserData.CreateStatic<float>();
            netTypes["string"] = UserData.CreateStatic<StringWrap>();
            apiTable["net"] = netTypes;

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
            UserData.RegisterType<Vector2>();
            unityTypes["vector2"] =
                UserData.CreateStatic<Vector2>();
            UserData.RegisterType<Vector3>();
            unityTypes["vector3"] =
                UserData.CreateStatic<Vector3>();
            UserData.RegisterType<Color>();
            unityTypes["color"] = UserData.CreateStatic<Color>();
            UserData.RegisterType<Angle>();
            unityTypes["angle"] = UserData.CreateStatic<Angle>();

            // Expose Unity enums
            Table unityEnums = new Table(session);
            UserData.RegisterType<LineCap>();
            unityTypes["lineCap"] = UserData.CreateStatic<LineCap>();
            UserData.RegisterType<LineJoin>();
            unityTypes["lineJoin"] = UserData.CreateStatic<LineJoin>();
            UserData.RegisterType<FillRule>();
            unityTypes["fillRule"] = UserData.CreateStatic<FillRule>();
            UserData.RegisterType<AngleUnit>();
            unityTypes["angleUnit"] = UserData.CreateStatic<
                AngleUnit>();
            UserData.RegisterType<ArcDirection>();
            unityTypes["arcDirection"] = UserData.CreateStatic<
                ArcDirection>();

            unityTypes["enum"] = unityEnums;
            apiTable["unity"] = unityTypes;

            // Expose utility classes
            Table utilTypes = new Table(session);
            utilTypes["style"] = UserData.CreateStatic<StyleHelper>();
            utilTypes["io"] = UserData.CreateStatic<IO>();
            apiTable["util"] = utilTypes;

            // Experimental: expose enums
            Table typeTable = new Table(session);
            UserData.RegisterType<KeyCode>();
            typeTable["KeyCode"] = UserData.CreateStatic<KeyCode>();
            apiTable["enums"] = typeTable;

            return apiTable;
        }
    }

    public class ApiNotSupportedException : Exception { }
}