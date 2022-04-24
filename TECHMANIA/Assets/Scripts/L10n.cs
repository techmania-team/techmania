using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

// String table format
//
// Row 0: header
// Row 1: language names
// Row 2: localizer names
// Subsequent rows:
//   Field 0 - comment
//   Field 1 - key
//   Same field as the locale in header - string content

// On keys
//
// In the system instance, keys can be any string as it's up to
// LocalizeString to match them.
//
// In the theme instance, keys come in 2 types:
// 1. a series of space-separated element names, including #
// 2. an arbitrary string that doesn't contain #
//
// For type 1, ThemeL10n will look for each element from the tree
// and replace its text.
// For type 2, ThemeL10n works the same way as the system instance.

[MoonSharp.Interpreter.MoonSharpUserData]
public class Locale
{
    public string languageName;
    public List<string> localizers;
    public Dictionary<string, string> strings;

    public Locale()
    {
        localizers = new List<string>();
        strings = new Dictionary<string, string>();
    }
}

// This class loads string tables and serves strings based on
// the current locale.
// There are 2 instances: one for the system, one for the theme.
public class L10n
{
    public enum Instance
    {
        System,
        Theme
    }
    public static L10n systemInstance;
    public static L10n themeInstance;

    // Static fields
    public static event UnityAction LocaleChanged;
    public Dictionary<string, Locale> locales;
    public Locale current { get; private set; }
    public const string kDefaultLocale = "en";

    private static L10n GetInstance(Instance instanceType)
    {
        return instanceType switch
        {
            Instance.System => systemInstance,
            Instance.Theme => themeInstance,
            _ => throw new System.Exception("Unsupported L10n instance: " + instanceType)
        };
    }

    public static void Initialize(
        string stringTable, Instance instanceType)
    {
        L10n instance = null;
        switch (instanceType)
        {
            case Instance.System:
                systemInstance = new L10n();
                instance = systemInstance;
                break;
            case Instance.Theme:
                themeInstance = new L10n();
                instance = themeInstance;
                break;
        }
        instance.locales = new Dictionary<string, Locale>();
        // instance.locals as a index-able list.
        List<Locale> localeList = new List<Locale>();

        NReco.Csv.CsvReader csvReader = new NReco.Csv.CsvReader(
            new StringReader(stringTable));
        // Header
        csvReader.Read();
        for (int i = 2; i < csvReader.FieldsCount; i++)
        {
            string localeName = csvReader[i];
            Locale l = new Locale();
            instance.locales.Add(localeName, l);
            localeList.Add(l);
        }

        // Language names
        csvReader.Read();
        for (int i = 2; i < csvReader.FieldsCount; i++)
        {
            localeList[i - 2].languageName = csvReader[i];
        }

        // Localizer names
        csvReader.Read();
        for (int i = 2; i < csvReader.FieldsCount; i++)
        {
            foreach (string name in csvReader[i].Split(','))
            {
                string trimmedName = name.Trim();
                if (trimmedName == "") continue;
                localeList[i - 2].localizers.Add(trimmedName);
            }
            localeList[i - 2].localizers.Sort();
        }

        // Strings
        int stringCount = 0;
        while (csvReader.Read())
        {
            string key = csvReader[1];
            if (key == "")
            {
                // Empty line
                continue;
            }
            stringCount++;
            for (int i = 2; i < csvReader.FieldsCount; i++)
            {
                localeList[i - 2].strings.Add(key, csvReader[i]);
            }
        }

        Debug.Log($"Loaded {stringCount} strings in {localeList.Count} locales.");
    }

    // This does not affect options.
    public static void SetLocale(string locale, Instance instanceType)
    {
        L10n instance = GetInstance(instanceType);
        if (!instance.locales.ContainsKey(locale))
        {
            throw new System.Exception("Locale not found: " + locale);
        }
        instance.current = instance.locales[locale];
        Debug.Log($"Setting locale to {locale}.");
        LocaleChanged?.Invoke();
    }
    
    public static Dictionary<string, string> GetLocaleToLanguageName(
        Instance instanceType)
    {
        Dictionary<string, string> d =
            new Dictionary<string, string>();
        foreach (KeyValuePair<string, Locale> pair in
            GetInstance(instanceType).locales)
        {
            d.Add(pair.Key, pair.Value.languageName);
        }
        return d;
    }

    public static Dictionary<string, List<string>>
        GetLanguageNameToLocalizerNames(Instance instanceType)
    {
        Dictionary<string, List<string>> d =
            new Dictionary<string, List<string>>();
        foreach (KeyValuePair<string, Locale> pair in
            GetInstance(instanceType).locales)
        {
            if (pair.Key == kDefaultLocale) continue;
            d.Add(pair.Value.languageName, pair.Value.localizers);
        }
        return d;
    }
    
    private static string GetStringFrom(string key,
        Locale locale)
    {
        if (!locale.strings.ContainsKey(key)) return null;
        if (locale.strings[key] == "") return null;
        return locale.strings[key];
    }

    public static string GetString(string key,
        Instance instanceType = Instance.System)
    {
        L10n instance = GetInstance(instanceType);
        if (instance == null || instance.current == null)
        {
            // String table not yet loaded, nothing we can do.
            return "";
        }

        string s = GetStringFrom(key, instance.current);
        if (s != null) return s;

        Debug.LogError("L10n key not found: " + key);
        return "";
    }

    public static string GetStringAndFormat(string formatKey,
        params object[] args)
    {
        string format = GetString(formatKey);
        return string.Format(format, args);
    }

    public static string GetStringAndFormatIncludingPaths(
        string formatKey, params object[] args)
    {
        return Paths.HidePlatformInternalPath(
            GetStringAndFormat(formatKey, args));
    }
}
