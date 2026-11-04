using System.Text.Json;
using System.Text.Json.Serialization;
using OpenTK.Mathematics;

namespace Qutre3D.Core;

/// <summary>Компактный формат вектора: [x, y, z] вместо кучи свойств OpenTK.</summary>
public sealed class Vector3JsonConverter : JsonConverter<Vector3>
{
    public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            float x = 0f, y = 0f, z = 0f;
            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                string name = reader.GetString();
                reader.Read();
                switch (name)
                {
                    case "X": x = reader.GetSingle(); break;
                    case "Y": y = reader.GetSingle(); break;
                    case "Z": z = reader.GetSingle(); break;
                    default: reader.Skip(); break;
                }
            }
            reader.Read();
            return new Vector3(x, y, z);
        }

        reader.Read();
        float vx = reader.GetSingle();
        reader.Read();
        float vy = reader.GetSingle();
        reader.Read();
        float vz = reader.GetSingle();
        reader.Read();
        return new Vector3(vx, vy, vz);
    }

    public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteNumberValue(value.Z);
        writer.WriteEndArray();
    }
}

public static class SceneIO
{
    public const string Extension = ".qutre";

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        IncludeFields = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new Vector3JsonConverter(),
            new JsonStringEnumConverter(),
        },
    };

    public static string Serialize(Scene scene) => JsonSerializer.Serialize(scene, Options);

    public static Scene Deserialize(string json)
    {
        var scene = JsonSerializer.Deserialize<Scene>(json, Options)
                    ?? throw new InvalidDataException("Файл сцены пуст или повреждён.");
        scene.Entities ??= new List<Entity>();
        scene.Sky ??= new SkySettings();
        scene.Fog ??= new FogSettings();
        scene.Post ??= new PostSettings();
        return scene;
    }

    public static void Save(Scene scene, string path) => File.WriteAllText(path, Serialize(scene));

    public static Scene Load(string path) => Deserialize(File.ReadAllText(path));
}
