using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

// A wrapper around Newtonsoft.Json.
public static class Json
{
    public static string Serialize(object obj, bool formatForFile)
    {
        Newtonsoft.Json.JsonSerializer serializer =
            new Newtonsoft.Json.JsonSerializer();
        serializer.Error += HandleError;
        StringBuilder stringBuilder = new StringBuilder();

        using (TextWriter textWriter = new StringWriter(stringBuilder))
        using (Newtonsoft.Json.JsonTextWriter writer =
            new Newtonsoft.Json.JsonTextWriter(textWriter))
        {
            if (formatForFile)
            {
                writer.Formatting = Newtonsoft.Json.Formatting.Indented;
                writer.Indentation = 1;
                writer.IndentChar = '\t';
            }
            else
            {
                writer.Formatting = Newtonsoft.Json.Formatting.None;
            }
            serializer.Serialize(writer, obj);
        }
        return stringBuilder.ToString();
    }

    public static T Deserialize<T>(string json) where T : class
    {
        return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(
            json, GetDeserializeSettings());
    }

    public static object Deserialize(string json, System.Type type)
    {
        return Newtonsoft.Json.JsonConvert.DeserializeObject(
            json, type, GetDeserializeSettings());
    }

    private static void HandleError(object sender,
        Newtonsoft.Json.Serialization.ErrorEventArgs e)
    {
        UnityEngine.Debug.LogError(e.ErrorContext.Error.Message);
    }

    private static Newtonsoft.Json.JsonSerializerSettings
        GetDeserializeSettings()
    {
        return new Newtonsoft.Json.JsonSerializerSettings()
        {
            Error = HandleError,
            ObjectCreationHandling =
                    Newtonsoft.Json.ObjectCreationHandling.Replace
        };
    }
}
