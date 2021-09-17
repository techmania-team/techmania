using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace FantomLib
{
    /// <summary>
    /// Color format conversion of Unity and Android (AARRGGBB: int32)
    /// [Unity.Color] https://docs.unity3d.com/ja/current/ScriptReference/Color.html
    /// [Android Color(=int32)] https://developer.android.com/reference/android/graphics/Color.html
    /// </summary>
    public static class XColor
    {
        /// <summary>
        /// Unity Color -> AARRGGBB format: int32
        /// </summary>
        /// <param name="color">UnityEngine.Color</param>
        /// <returns>AARRGGBB format: int32</returns>
        public static int ToIntARGB(this Color color)
        {
            //string htmlColor = ColorUtility.ToHtmlStringRGBA(color);    //"RRGGBBAA"
            //int r = RedValue(htmlColor);
            //int g = GreenValue(htmlColor);
            //int b = BlueValue(htmlColor);
            //int a = AlphaValue(htmlColor);
            int r = RedValue(color);
            int g = GreenValue(color);
            int b = BlueValue(color);
            int a = AlphaValue(color);
            return (a << 24) | (r << 16) | (g << 8) | b;    //AARRGGBB
        }


        /// <summary>
        /// HTML Color ("#RRGGBBAA" etc.) -> AARRGGBB format: int32
        /// (Format)
        /// [StartsWith] "0x~", "#~", (nothing)
        /// [Color part] "ffffffff", "ffffff", "ffff", "fff" (8, 6, 4, 3 characters)
        /// </summary>
        /// <param name="color">UnityEngine.Color</param>
        /// <returns>AARRGGBB format: int32</returns>
        public static int ToIntARGB(string htmlString)
        {
            int r = RedValue(htmlString);
            int g = GreenValue(htmlString);
            int b = BlueValue(htmlString);
            int a = AlphaValue(htmlString);
            return (a << 24) | (r << 16) | (g << 8) | b;    //AARRGGBB
        }


        /// <summary>
        /// Each color part(0~255) -> AARRGGBB format: int32
        /// </summary>
        /// <param name="r">Red part (0~255)</param>
        /// <param name="g">Green part (0~255)</param>
        /// <param name="b">Blue part (0~255)</param>
        /// <param name="a">Alpha part (0~255)</param>
        /// <returns>AARRGGBB format: int32</returns>
        public static int ToIntARGB(int r, int g, int b, int a = 255)
        {
            return ((a & 0xff) << 24) | ((r & 0xff) << 16) | ((g & 0xff) << 8) | (b & 0xff);    //AARRGGBB
        }


        /// <summary>
        /// Unity Color -> extract Red part: int
        /// </summary>
        /// <param name="color">UnityEngine.Color</param>
        /// <returns>Red part: int (0~255)</returns>
        public static int RedValue(this Color color)
        {
            //string htmlColor = ColorUtility.ToHtmlStringRGBA(color);    //"RRGGBBAA"
            //return RedValue(htmlColor);
            return Mathf.RoundToInt(color.r * 255);
        }


        /// <summary>
        /// Unity Color -> extract Green part: int
        /// </summary>
        /// <param name="color">UnityEngine.Color</param>
        /// <returns>Green part: int (0~255)</returns>
        public static int GreenValue(this Color color)
        {
            //string htmlColor = ColorUtility.ToHtmlStringRGBA(color);    //"RRGGBBAA"
            //return GreenValue(htmlColor);
            return Mathf.RoundToInt(color.g * 255);
        }


        /// <summary>
        /// Unity Color -> extract Blue part: int
        /// </summary>
        /// <param name="color">UnityEngine.Color</param>
        /// <returns>Blue part: int (0~255)</returns>
        public static int BlueValue(this Color color)
        {
            //string htmlColor = ColorUtility.ToHtmlStringRGBA(color);    //"RRGGBBAA"
            //return BlueValue(htmlColor);
            return Mathf.RoundToInt(color.b * 255);
        }


        /// <summary>
        /// Unity Color > extract Alpha part: int
        /// </summary>
        /// <param name="color">UnityEngine.Color</param>
        /// <returns>Alpha part: int (0~255)</returns>
        public static int AlphaValue(this Color color)
        {
            //string htmlColor = ColorUtility.ToHtmlStringRGBA(color);    //"RRGGBBAA"
            //return AlphaValue(htmlColor);
            return Mathf.RoundToInt(color.a * 255);
        }



        /// <summary>
        /// Extract the Color part
        /// (Format)
        /// [StartsWith] "0x~", "#~", (nothing)
        /// [Color part] "ffffffff", "ffffff", "ffff", "fff" (8, 6, 4, 3 characters)
        /// </summary>
        /// <param name="htmlString">HTML Color</param>
        /// <returns>Extracted color part ("ffffffff", "ffffff", "ffff", "fff" etc.) / nothing -> ""</returns>
        public static string GetColorCodeString(string htmlString)
        {
            if (htmlString.ToLower().StartsWith("0x"))
                htmlString = htmlString.Substring(2);
            else if (htmlString.StartsWith("#"))
                htmlString = htmlString.Substring(1);

            if (!Regex.IsMatch(htmlString, "^[0-9a-fA-F]{3,8}$"))
                return "";
            if (htmlString.Length == 5 || htmlString.Length == 7)
                return "";

            return htmlString;
        }


        /// <summary>
        /// HTML Color ("#RRGGBBAA" etc.) -> extract Red part: int
        /// (Format)
        /// [StartsWith] "0x~", "#~", (nothing)
        /// [Color part] "ffffffff", "ffffff", "ffff", "fff" (8, 6, 4, 3 characters)
        /// </summary>
        /// <param name="htmlString">HTML Color</param>
        /// <returns>Red part: int (0~255) / nothing -> 0</returns>
        public static int RedValue(string htmlString)
        {
            htmlString = GetColorCodeString(htmlString);
            if (string.IsNullOrEmpty(htmlString))
                return 0;

            if (htmlString.Length == 8 || htmlString.Length == 6)  //"RRGGBBAA" or "RRGGBB"
            {
                string hex = htmlString.Substring(0, 2);
                return Convert.ToInt32(hex, 16);
            }
            if (htmlString.Length == 4 || htmlString.Length == 3)  //"RGBA" or "RGB"
            {
                string hex = htmlString.Substring(0, 1);
                return Convert.ToInt32(hex + hex, 16);
            }
            return 0;
        }


        /// <summary>
        /// HTML Color ("#RRGGBBAA" etc.)-> extract Green part: int
        /// (Format)
        /// [StartsWith] "0x~", "#~", (nothing)
        /// [Color part] "ffffffff", "ffffff", "ffff", "fff" (8, 6, 4, 3 characters)
        /// </summary>
        /// <param name="htmlString">HTML Color</param>
        /// <returns>Green part: int (0~255)/nothing -> 0</returns>
        public static int GreenValue(string htmlString)
        {
            htmlString = GetColorCodeString(htmlString);
            if (string.IsNullOrEmpty(htmlString))
                return 0;

            if (htmlString.Length == 8 || htmlString.Length == 6)  //"RRGGBBAA" or "RRGGBB"
            {
                string hex = htmlString.Substring(2, 2);
                return Convert.ToInt32(hex, 16);
            }
            if (htmlString.Length == 4 || htmlString.Length == 3)  //"RGBA" or "RGB"
            {
                string hex = htmlString.Substring(1, 1);
                return Convert.ToInt32(hex + hex, 16);
            }
            return 0;
        }


        /// <summary>
        /// HTML Color ("#RRGGBBAA" etc.)-> extract Blue part: int
        /// (Format)
        /// [StartsWith] "0x~", "#~", (nothing)
        /// [Color part] "ffffffff", "ffffff", "ffff", "fff" (8, 6, 4, 3 characters)
        /// </summary>
        /// <param name="htmlString">HTML Color</param>
        /// <returns>Blue part: int (0~255) / nothing -> 0</returns>
        public static int BlueValue(string htmlString)
        {
            htmlString = GetColorCodeString(htmlString);
            if (string.IsNullOrEmpty(htmlString))
                return 0;

            if (htmlString.Length == 8 || htmlString.Length == 6)  //"RRGGBBAA" or "RRGGBB"
            {
                string hex = htmlString.Substring(4, 2);
                return Convert.ToInt32(hex, 16);
            }
            if (htmlString.Length == 4 || htmlString.Length == 3)  //"RGBA" or "RGB"
            {
                string hex = htmlString.Substring(2, 1);
                return Convert.ToInt32(hex + hex, 16);
            }
            return 0;
        }


        /// <summary>
        /// HTML Color ("#RRGGBBAA" etc.)-> extract Alpha part: int
        /// (Format)
        /// [StartsWith] "0x~", "#~", (nothing)
        /// [Color part] "ffffffff", "ffffff" (8, 4文字) / "ffff", "fff" (6, 3 characters -> 255)
        /// </summary>
        /// <param name="htmlString">HTML Color</param>
        /// <returns>Alpha part: int (0~255) / nothing -> 0</returns>
        public static int AlphaValue(string htmlString)
        {
            htmlString = GetColorCodeString(htmlString);
            if (string.IsNullOrEmpty(htmlString))
                return 0;

            if (htmlString.Length == 6 || htmlString.Length == 3)  //"RRGGBB", "RGB"
            {
                return 0xff;
            }
            if (htmlString.Length == 8)  //"RRGGBBAA"
            {
                string hex = htmlString.Substring(6, 2);
                return Convert.ToInt32(hex, 16);
            }
            if (htmlString.Length == 4)  //"RGBA"
            {
                string hex = htmlString.Substring(3, 1);
                return Convert.ToInt32(hex + hex, 16);
            }
            return 0;
        }



        /// <summary>
        /// AARRGGBB format: int32 -> Unity Color
        /// </summary>
        /// <param name="argb">AARRGGBB format: int32</param>
        /// <returns>Unity Color format (Conversion failure -> Color.clear)</returns>
        public static Color ToColor(int argb)
        {
            //string htmlString = ToHtmlString(argb);
            //Color color;
            //if (ColorUtility.TryParseHtmlString(htmlString, out color))
            //    return color;
            //return Color.clear;

            int r = RedValue(argb);
            int g = GreenValue(argb);
            int b = BlueValue(argb);
            int a = AlphaValue(argb);
            return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
        }


        /// <summary>
        /// Each color part(0~255) -> Unity Color
        /// </summary>
        /// <param name="r">Red part (0~255)</param>
        /// <param name="g">Green part (0~255)</param>
        /// <param name="b">Blue part (0~255)</param>
        /// <param name="a">Alpha part (0~255)</param>
        /// <returns>Unity Color format</returns>
        public static Color ToColor(int r, int g, int b, int a = 255)
        {
            //return ToColor(ToIntARGB(r, g, b, a));
            return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
        }


        /// <summary>
        /// HTML Color ("#RRGGBBAA" etc.)-> Unity Color
        /// (Format)
        /// [StartsWith] "0x~", "#~", (nothing)
        /// [Color part] "ffffffff", "ffffff", "ffff", "fff" (8, 6, 4, 3 characters)
        /// </summary>
        /// <param name="argb">AARRGGBB format: int32</param>
        /// <returns>Unity Color format</returns>
        public static Color ToColor(string htmlString)
        {
            //return ToColor(ToIntARGB(htmlString));

            int r = RedValue(htmlString);
            int g = GreenValue(htmlString);
            int b = BlueValue(htmlString);
            int a = AlphaValue(htmlString);
            return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
        }


        /// <summary>
        /// AARRGGBB format: int32 -> HTML Color
        /// </summary>
        /// <param name="argb">AARRGGBB format: int32</param>
        /// <param name="addSharp">Add '#' to the head and return it (default)</param>
        /// <returns>HTML Color ("#ffffffff", "ffffffff" etc.)</returns>
        public static string ToHtmlString(int argb, bool addSharp = true)
        {
            int r = RedValue(argb);
            int g = GreenValue(argb);
            int b = BlueValue(argb);
            int a = AlphaValue(argb);
            string htmlString = r.ToString("x2") + g.ToString("x2") + b.ToString("x2") + a.ToString("x2");  //"RRGGBBAA"
            return addSharp ? ("#" + htmlString) : htmlString;
        }


        /// <summary>
        /// AARRGGBB format: int32 -> extract Red part: int
        /// </summary>
        /// <param name="argb">AARRGGBB format: int32</param>
        /// <returns>Red part: int (0~255)</returns>
        public static int RedValue(int argb)
        {
            return ((argb & 0x00ff0000) >> 16);
        }


        /// <summary>
        /// AARRGGBB format: int32 -> extract Green part: int
        /// </summary>
        /// <param name="argb">AARRGGBB format: int32</param>
        /// <returns>Green part: int (0~255)</returns>
        public static int GreenValue(int argb)
        {
            return ((argb & 0x0000ff00) >> 8);
        }


        /// <summary>
        /// AARRGGBB format: int32-> extract Blue part: int
        /// </summary>
        /// <param name="argb">AARRGGBB format: int32</param>
        /// <returns>Blue part: int (0~255)</returns>
        public static int BlueValue(int argb)
        {
            return (argb & 0x000000ff);
        }


        /// <summary>
        /// AARRGGBB format: int32-> extract Alpha part: int
        /// </summary>
        /// <param name="argb">AARRGGBB format: int32</param>
        /// <returns>Alpha part: int (0~255)</returns>
        public static int AlphaValue(int argb)
        {
            return ((argb >> 24) & 0x000000ff);
        }

    }
}