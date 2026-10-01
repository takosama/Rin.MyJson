using Rin.MyJson;
using System.Globalization;
using System.Text.Json;

int count = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); count++; }
void Reject(string text) { try { Deserializer.DeserializeJson(text); } catch (JsonException) { count++; return; } throw new Exception("Accepted malformed JSON: " + text); }
string[] strings = { "", "x\",\"extra\":true,\"tail\":\"y", "[a],{b}:c", "quote\" slash\\ newline\n tab\t", "日本語💞", "\\n", "\0\b\f\r" };
foreach (var text in strings)
{
    var obj = new JsonObject(); obj.Add(text, text);
    string serialized = obj.ToJsonText();
    using var standard = JsonDocument.Parse(serialized);
    Check(standard.RootElement.EnumerateObject().Count() == 1, "Property injection");
    Check(standard.RootElement.GetProperty(text).GetString() == text, "Standard roundtrip");
    var restored = Deserializer.DeserializeJson(serialized);
    Check((string)restored[text] == text, "Wrapper roundtrip");
    Check((string)new JsonValue(text) == text, "Literal constructor must not regex-unescape");
}
foreach (var culture in new[] { "en-US", "fr-FR", "de-DE" })
{
    CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
    var obj = new JsonObject(); obj.Add("n", 12.5m);
    Check(obj.ToJsonText().Contains("12.5"), "Culture-independent output");
    Check((decimal)Deserializer.DeserializeJson(" { \"n\" : 1.25e1 } ")["n"].Value == 12.5m, "Whitespace/exponent");
}
var nested = Deserializer.DeserializeJson("{\"a\":[null,true,false,{\"b\":\"[\\\"],x\"}],\"empty\":{},\"arr\":[]}");
Check(nested["a"][0].Value is null && (bool)nested["a"][1].Value, "Nested types");
Check(nested["a"][3]["b"].ToString() == "[\"],x", "Quoted delimiters");
Check(Deserializer.DeserializeJson("{\"k\":1,\"k\":2}")["k"].ToString() == "2", "Duplicate-key compatibility");
foreach (var malformed in new[] { "", " ", "[]", "null", "{", "{\"x\":}", "{\"x\":truefoo}", "{\"x\":\"\\q\"}", "{\"x\":1,}", "{\"x\":1e999}" }) Reject(malformed);
Reject(new string('{', 100));
var boundary = "{\"x\":" + new string('[', 63) + "0" + new string(']', 63) + "}";
Check(Deserializer.DeserializeJson(boundary).ToJsonText() == boundary, "Depth 64 roundtrip");
Reject("{\"x\":" + new string('[', 64) + "0" + new string(']', 64) + "}");
var cycle = new JsonObject(); cycle.Add("self", cycle);
try { cycle.ToJsonText(); throw new Exception("Accepted cycle"); } catch (JsonException) { count++; }
var directArray = new JsonArray("quote\" and \\ and \n", JsonValue.Null, new JsonArray("nested", true), (JsonValue)null);
using (var parsed = JsonDocument.Parse(directArray.ToString()))
{
    Check(parsed.RootElement[0].GetString() == "quote\" and \\ and \n", "Direct array escapes strings");
    Check(parsed.RootElement[1].ValueKind == JsonValueKind.Null && parsed.RootElement[3].ValueKind == JsonValueKind.Null, "Array null entries");
    Check(parsed.RootElement[2][0].GetString() == "nested", "Nested direct array");
}
Check(new JsonValue(directArray).ToString() == directArray.ToString(), "JsonValue array delegates safe writer");
Check(new JsonArray().ToString() == "[]", "Empty direct array");
var cyclicArray = new JsonArray(JsonValue.Null);
cyclicArray.Array[0] = new JsonValue(cyclicArray);
try { cyclicArray.ToString(); throw new Exception("Accepted array cycle"); } catch (JsonException) { count++; }
Console.WriteLine($"PASS {count} checks");
