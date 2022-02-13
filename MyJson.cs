using System.Text;

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
        static ReadOnlySpan<char> _GetObject(char Separator, ReadOnlySpan<char> JsonText, out ReadOnlySpan<char> next)
        {
            List<(string key, JsonValue val)> list = new List<(string key, JsonValue val)>();
            bool IsInStr = false;
            bool IsInArr = false;
            bool IsInObj = false;
            for (int i = 0; i < JsonText.Length; i++)
            {
                if (JsonText[i] == '\"')
                    IsInStr = !IsInStr;
                if (!IsInStr && JsonText[i] == '[')
                    IsInArr = true;
                if (!IsInStr && JsonText[i] == ']')
                    IsInArr = false;
                if (!IsInStr && JsonText[i] == '{')
                    IsInObj = true;
                if (!IsInStr && JsonText[i] == '}')
                    IsInObj = false;


                if (!IsInStr && !IsInArr && !IsInObj && JsonText[i] == Separator)
                {
                    next = JsonText.Slice(i + 1);
                    return JsonText.Slice(0, i);
                }
            }
            next = null;
            return JsonText;
        }
        static JsonObject _ToObject(ReadOnlySpan<char> JsonText)
        {
            JsonObject rtn = new JsonObject();
            ReadOnlySpan<char> next = JsonText;
            while (!next.IsEmpty)
            {
                var obj = _GetObject(',', next, out next);

                var key = _ToKey(obj, out var vstr);
                var val = _ToValue(vstr);
                rtn.Add(key, val);
            }

            return rtn;
        }
        static string _ToKey(ReadOnlySpan<char> JsonText, out ReadOnlySpan<char> val)
        {
            var obj = _GetObject(':', JsonText, out val);
            return obj[1..^1].ToString();
        }
        static JsonArray _ToArray(ReadOnlySpan<char> JsonText)
        {
            var str = JsonText;
            List<JsonValue> list = new List<JsonValue>();
            while (!str.IsEmpty)
            {
                var o = _GetObject(',', str, out str);
                list.Add(_ToValue(o));
            }
            return new JsonArray(list.ToArray());
        }
        static JsonValue _ToValue(ReadOnlySpan<char> JsonText)
        {
            return JsonText[0] switch
            {
                '\"' => new JsonValue(JsonText[1..^1]),
                '[' => new JsonValue(_ToArray(JsonText[1..^1])),
                '{' => new JsonValue(_ToObject(JsonText[1..^1])),
                'f' => new JsonValue(false),
                't' => new JsonValue(true),
                'n' => JsonValue.Null,
                _ => new JsonValue(decimal.Parse(JsonText))
            };
        }
        public static JsonObject DeserializeJson(ReadOnlySpan<char> JsonText)
        {
            if (JsonText[0] == '{' && JsonText[JsonText.Length - 1] == '}')
            {
                var tmp = _ToObject(JsonText[1..^1]);
                return tmp;
            }
            else
            {
                throw new Exception();
            }
        }
    }
    public static class Serializer
    {

        public static string SerializeJson(JsonObject json)
        {
            var rtn = json.ToJString();
            return rtn;
        }

        static string ToJString(this JsonObject json)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append('{');
            int cnt = json.Dic.Count;
            if (cnt > 0)
            {
                var e = json.Dic.GetEnumerator();
                for (int i = 0; i < cnt - 1; i++)
                {
                    e.MoveNext();
                    sb.Append('\"');
                    sb.Append(e.Current.Key);
                    sb.Append('\"');
                    sb.Append(':');
                    sb.Append(e.Current.Value.ToJString());
                    sb.Append(',');
                }
                e.MoveNext();
                sb.Append('\"');
                sb.Append(e.Current.Key);
                sb.Append('\"');
                sb.Append(':');
                sb.Append(e.Current.Value.ToJString());
            }
            sb.Append('}');
            var rtn = sb.ToString();
            return rtn;
        }

        static string ToJString(this JsonValue val)
        {
            return val.Value switch
            {
                string value => $"\"{value}\"",
                bool value => value ? "true" : "false",
                decimal value => value.ToString(),
                null => "null",
                JsonArray array => ToJString(array),
                JsonObject obj => ToJString(obj),
                _ => ""
            };
        }

        static string ToJString(this JsonArray arr)
        {
            var sb = new StringBuilder();
            sb.Append('[');
            if (arr.Array.Length > 0)
            {
                for (int i = 0; i < arr.Array.Length - 1; i++)
                {
                    sb.Append(arr.Array[i].ToJString());
                    sb.Append(',');
                }
                sb.Append(arr.Array[arr.Array.Length - 1].ToJString());
            }
            sb.Append(']');

            return sb.ToString();
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

        public override string ToString()
        {
            return Value switch
            {
                string value => value,
                bool value => value ? "true" : "false",
                decimal value => value.ToString(),
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
    }
    public class JsonObject
    {
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


        public JsonObject()
        {
            Dic = new Dictionary<string, JsonValue>();
        }
    }
}
