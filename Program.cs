using Rin.MyJson;

Console.WriteLine( new JsonValue("str"));
Console.WriteLine(new JsonValue(114m));
Console.WriteLine(new JsonValue(false));
Console.WriteLine(new JsonValue(true));
Console.WriteLine(new JsonValue());
Console.WriteLine(new JsonArray("ids",114m,156m));

Console.WriteLine(new JsonObject(new Dictionary<string, JsonValue>() { { "name", "rin💞" }, { "ids", new JsonArray("ids", 114m, 514m) } }));

var tmp = new JsonObject(new Dictionary<string, JsonValue>() { { "name",  "rin💞" }, { "ids", new JsonArray("ids", 114m, 514m) } });

Console.WriteLine(new JsonObject(new Dictionary<string, JsonValue>() { { "root", tmp } }));

var json = new JsonObject();
json.Add("name", "rin💞");
json.Add("idstr", "rin_sns");
var json2 = new JsonObject();
json2.Add("pic", "item.jpg");
json2.Add("pic2", "item2.jpg");
var json3 = new JsonObject();
json3.Add("url", new JsonArray("item.,jpg", "item2.png"));
json2.Add("urls", json3);
json.Add("friends", new JsonArray(114, 514, 1919,JsonValue.Null,"",true, json2));
Console.WriteLine(json);


var str = "{\"name\":\"rin\uD83D\uDC9E\",\"idstr\":\"rin_sns\",\"friends\":[114,514,1919,null,\"\",true,{\"pic\":\"item.jpg\",\"pic2\":\"item2.jpg\",\"urls\":{\"url\":[\"item.jpg\",\"item2.png\"]}}]}";
var jobj=Deserializer.DeserializeJson(str);
Console.WriteLine(jobj.ToJsonText());

var id = jobj["name"];
var f= jobj["friends"][6]["urls"]["url"];


Console.WriteLine(id);
