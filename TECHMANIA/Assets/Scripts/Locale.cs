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

public class Locale
{
    // Instance fields
    private string languageName;
    private List<string> localizers;
    private Dictionary<string, string> strings;
    public Locale()
    {
        localizers = new List<string>();
        strings = new Dictionary<string, string>();
    }

    // Static fields
    public static event UnityAction LocaleChanged;
    private static Dictionary<string, Locale> locales;
    private static Locale current;
    private static Locale fallback;
    public const string kDefaultLocale = "en";

    public static void Initialize(TextAsset stringTable)
    {
        NReco.Csv.CsvReader csvReader = new NReco.Csv.CsvReader(
            new StringReader(stringTable.text));
        locales = new Dictionary<string, Locale>();
        List<Locale> localeList = new List<Locale>();

        // Header
        csvReader.Read();
        for (int i = 2; i < csvReader.FieldsCount; i++)
        {
            string localeName = csvReader[i];
            Locale l = new Locale();
            locales.Add(localeName, l);
            localeList.Add(l);

            if (localeName == kDefaultLocale)
            {
                fallback = l;
            }
        }
        if (fallback == null)
        {
            throw new System.Exception("Default locale not found.");
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

    public static void SetLocale(string locale)
    {
        if (!locales.ContainsKey(locale))
        {
            throw new System.Exception("Locale not found: " + locale);
        }
        current = locales[locale];
        Debug.Log($"Setting locale to {locale}.");
        LocaleChanged?.Invoke();
    }
    
    public static Dictionary<string, string> GetLocaleToLanguageName()
    {
        Dictionary<string, string> d = new Dictionary<string, string>();
        foreach (KeyValuePair<string, Locale> pair in locales)
        {
            d.Add(pair.Key, pair.Value.languageName);
        }
        return d;
    }

    public static Dictionary<string, List<string>>
        GetLanguageNameToLocalizerNames()
    {
        Dictionary<string, List<string>> d =
            new Dictionary<string, List<string>>();
        foreach (KeyValuePair<string, Locale> pair in locales)
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

    public static string GetString(string key)
    {
        if (current == null)
        {
            // String table not yet loaded, nothing we can do.
            return "";
        }

        string s = GetStringFrom(key, current);
        if (s != null) return s;

        s = GetStringFrom(key, fallback);
        if (s == null)
        {
            Debug.LogError("Key not found: " + key);
            s = "";
        }
        return s;
    }

    public static string GetStringAndFormat(string formatKey,
        params object[] args)
    {
        string format = GetString(formatKey);
        return string.Format(format, args);
    }
}
