using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;

namespace ThemeApi
{
    // Because MoonSharp converts .Net strings to Lua strings when
    // returning them to Lua code, we are unable to call .Net string
    // instance methods. This class wraps around .Net string and
    // routes instance methods via static ones.
    [MoonSharpUserData]
    public class StringWrap
    {
        public static int Length(string s)
        {
            return s.Length;
        }

        public static bool Contains(string s, string value)
        {
            return s.Contains(value);
        }

        public static bool EndsWith(string s, string value)
        {
            return s.EndsWith(value);
        }

        public static string Format(string format, string arg)
        {
            return string.Format(format, arg);
        }

        public static string Format(string format, double arg)
        {
            return string.Format(format, arg);
        }

        public static string Format(string format, string arg1,
            string arg2)
        {
            return string.Format(format, arg1, arg2);
        }

        public static string Format(string format, string arg1,
            string arg2, string arg3)
        {
            return string.Format(format, arg1, arg2, arg3);
        }

        public static string Format(string format, string arg1,
            string arg2, string arg3, string arg4)
        {
            return string.Format(format, arg1, arg2, arg3, arg4);
        }

        public static int IndexOf(string s, string value)
        {
            return s.IndexOf(value);
        }

        public static int IndexOf(string s, string value,
            int startIndex)
        {
            return s.IndexOf(value, startIndex);
        }

        public static string Insert(string s, int startIndex,
            string value)
        {
            return s.Insert(startIndex, value);
        }

        public static string Join(string separator, string[] values)
        {
            return string.Join(separator, values);
        }

        public static int LastIndexOf(string s, string value)
        {
            return s.LastIndexOf(value);
        }

        public static int LastIndexOf(string s, string value,
            int startIndex)
        {
            return s.LastIndexOf(value, startIndex);
        }

        public static string PadLeft(string s, int totalWidth)
        {
            return s.PadLeft(totalWidth);
        }

        public static string PadLeft(string s, int totalWidth,
            string paddingChar)
        {
            return s.PadLeft(totalWidth, paddingChar[0]);
        }

        public static string PadRight(string s, int totalWidth)
        {
            return s.PadRight(totalWidth);
        }

        public static string PadRight(string s, int totalWidth,
            string paddingChar)
        {
            return s.PadRight(totalWidth, paddingChar[0]);
        }

        public static string Remove(string s, int startIndex,
            int count)
        {
            return s.Remove(startIndex, count);
        }

        public static string Replace(string s, string oldValue,
            string newValue)
        {
            return s.Replace(oldValue, newValue);
        }

        public static string[] Split(string s, string separator)
        {
            return s.Split(separator);
        }

        public static bool StartsWith(string s, string value)
        {
            return s.StartsWith(value);
        }

        public static string Substring(string s, int startIndex)
        {
            return s.Substring(startIndex);
        }

        public static string Substring(string s, int startIndex,
            int length)
        {
            return s.Substring(startIndex, length);
        }

        public static string ToLower(string s)
        {
            return s.ToLower();
        }

        public static string ToUpper(string s)
        {
            return s.ToUpper();
        }

        public static string Trim(string s)
        {
            return s.Trim();
        }

        public static string TrimEnd(string s)
        {
            return s.TrimEnd();
        }

        public static string TrimStart(string s)
        {
            return s.TrimStart();
        }
    }
}