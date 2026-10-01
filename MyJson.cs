using System.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text.Json;

namespace Rin.MyJson
{
    public static class MyJsonEx
    {
        public static JsonObject ToJsonObject(this ReadOnlySpan<char> json)
        {
            return MyJson.Deserializer.DeserializeJson(json);
        }

        public static string ToJsonText(this JsonObject json)
        {
            return MyJson.Serializer.SerializeJson(json);
        }
    }
    public static class Deserializer
    {
        public static JsonObject DeserializeJson(ReadOnlySpan<char> JsonText)
        {
            // Preserve the public wrapper API and its decimal number model while
            // delegating JSON grammar and escape handling to the standard parser.
            using var document = JsonDocument.Parse(JsonText.ToString(), new JsonDocumentOptions { MaxDepth = 64 });
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new JsonException("The root JSON value must be an object.");
            return ReadObject(document.RootElement);
        }

        private static JsonObject ReadObject(JsonElement element)
        {
            var result = new JsonObject();
            foreach (var property in element.EnumerateObject())
                result.Add(property.Name, ReadValue(property.Value));
            // JsonObject.Add historically uses last-value-wins for duplicate keys.
            return result;
        }

        private static JsonValue ReadValue(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String: return new JsonValue(element.GetString());
                case JsonValueKind.Number:
                    if (!element.TryGetDecimal(out var number))
                        throw new JsonException("Number is outside the supported decimal range.");
                    return new JsonValue(number);
                case JsonValueKind.True: return new JsonValue(true);
                case JsonValueKind.False: return new JsonValue(false);
                case JsonValueKind.Null: return JsonValue.Null;
                case JsonValueKind.Object: return new JsonValue(ReadObject(element));
                case JsonValueKind.Array:
                    return new JsonValue(new JsonArray(element.EnumerateArray().Select(ReadValue).ToArray()));
                default: throw new JsonException("Unsupported JSON value.");
            }
        }
    }

    public static class Serializer
    {
        public static string SerializeJson(JsonObject json)
        {
            if (json is null) throw new ArgumentNullException(nameof(json));
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
                WriteObject(writer, json, 0);
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        internal static string SerializeArray(JsonArray json)
        {
            if (json is null) throw new ArgumentNullException(nameof(json));
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
                WriteValue(writer, new JsonValue(json), 0);
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private static void WriteObject(Utf8JsonWriter writer, JsonObject json, int depth)
        {
            CheckDepth(depth);
            writer.WriteStartObject();
            foreach (var pair in json.Dic)
            {
                writer.WritePropertyName(pair.Key);
                WriteValue(writer, pair.Value, depth + 1);
            }
            writer.WriteEndObject();
        }

        private static void WriteValue(Utf8JsonWriter writer, JsonValue value, int depth)
        {
            switch (value?.Value)
            {
                case null: writer.WriteNullValue(); break;
                case string text: writer.WriteStringValue(text); break;
                case bool boolean: writer.WriteBooleanValue(boolean); break;
                case decimal number: writer.WriteNumberValue(number); break;
                case JsonObject obj: WriteObject(writer, obj, depth); break;
                case JsonArray array:
                    CheckDepth(depth);
                    writer.WriteStartArray();
                    foreach (var item in array.Array) WriteValue(writer, item, depth + 1);
                    writer.WriteEndArray();
                    break;
                default: throw new JsonException("Unsupported JSON value.");
            }
        }

        private static void CheckDepth(int depth)
        {
            // Also bounds cycles in publicly mutable JsonObject.Dic / JsonArray.Array.
            if (depth >= 64) throw new JsonException("Maximum JSON depth exceeded.");
        }
    }
    public class JsonValue
    {
        public object Value { get; private set; }

        public bool IsArray
        {
            get
            {
                return Value switch
                {
                    JsonArray arr => true,
                    _ => false
                };
            }
        }

        public bool IsDictionary
        {
            get
            {
                return Value switch
                {
                    JsonObject dic => true,
                    _ => false
                };
            }
        }
        public JsonObject Dic
        {
            get
            {
                return Value switch
                {
                    JsonObject dic => dic,
                    _ => null
                };
            }
        }

        public JsonValue[] Array
        {
            get
            {
                return Value switch
                {
                    JsonArray arr => arr.Array,
                    _ => null
                };
            }
        }

        public static implicit operator string(JsonValue val)
        {
            return val.ToString();
        }


        public override string ToString()
        {
            return Value switch
            {
                string value => value,
                bool value => value ? "true" : "false",
                decimal value => value.ToString(CultureInfo.InvariantCulture),
                null => null,
                JsonArray arr => arr.ToString(),
                JsonObject obj => obj.ToString(),
                _ => ""
            };
        }



        public JsonValue this[string key]
        {
            get
            {
                return Value switch
                {
                    JsonObject obj => obj.Dic[key],
                    _ => JsonValue.Null
                };
            }
        }
        public JsonValue this[int index]
        {
            get
            {
                return Value switch
                {
                    JsonArray arr => arr.Array[index],
                    _ => JsonValue.Null
                };
            }
        }

        public JsonValue(string str)
        {
            this.Value = str;
        }

        public JsonValue(ReadOnlySpan<char> str)
        {
            this.Value = str.ToString();
        }

        public JsonValue(decimal num)
        {
            this.Value = num;
        }

        public JsonValue(bool val)
        {
            this.Value = val;
        }

        public JsonValue(JsonArray arr)
        {
            this.Value = arr;
        }

        public JsonValue(JsonObject obj)
        {
            this.Value = obj;
        }

        public JsonValue()
        {
            this.Value = null;
        }

        public static implicit operator JsonValue(string str) => new JsonValue(str);
        public static implicit operator JsonValue(decimal num) => new JsonValue(num);
        public static implicit operator JsonValue(bool val) => new JsonValue(val);
        public static implicit operator JsonValue(JsonArray arr) => new JsonValue(arr);
        public static implicit operator JsonValue(JsonObject obj) => new JsonValue(obj);
        public static JsonValue Null { get { return new JsonValue(); } }
    }
    public class JsonArray
    {
        public JsonValue[] Array { get; private set; }

        public JsonArray(params JsonValue[] values)
        {
            this.Array = values;
        }

        public override string ToString()
        {
            return Serializer.SerializeArray(this);
        }

    }
    public class JsonObject
    {
        public override string ToString()
        {
            return this.ToJsonText();
        }

        public Dictionary<string, JsonValue> Dic { get; private set; }

        public JsonValue this[string key]
        {
            get
            {
                if (this.Dic.TryGetValue(key, out var val))
                {
                    return val;
                }
                else
                    return JsonValue.Null;
            }
        }

        public JsonObject(Dictionary<string, JsonValue> dic)
        {
            this.Dic = dic;
        }

        public void Add(string key, JsonValue val)
        {
            this.Dic[key] = val;
        }

        public void Add(params (string key, JsonValue val)[] arr)
        {
            foreach (var item in arr)
                this.Dic[item.key] = item.val;
        }
        public void Add((string key, JsonValue val) val)
        {
            this.Dic[val.key] = val.val;
        }

        public JsonObject Remove(string key)
        {
            this.Dic.Remove(key);
            return this;
        }

        public JsonObject Remove(params string[] key)
        {
            foreach (var n in key)
                this.Dic.Remove(n);
            return this;
        }

        public JsonObject()
        {
            Dic = new Dictionary<string, JsonValue>();
        }
    }
}

