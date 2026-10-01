# Rin.MyJson
c#で動くJson解析ライブラリです



りんちゃにお布施をください<br/>
amazon<br/>
https://www.amazon.co.jp/hz/wishlist/ls/IMC1G88FCO7X?ref_=wl_share<br/>
btc/bch<br/>
1LdgA778k2eZ7sZwdbZus62LgrwcHPnxi1<br/>
ltc<br/>
LerdRKQxpgtcNgG6ojZD9766u5JtVVEKRG<br/>
eth<br/>
0x35B9f8Bb120e1a22cD4AC31B7<br/>

## JSON互換性と回帰テスト

パーサーとシリアライザーはSystem.Text.Jsonを使用します。既存のJsonObject/JsonArray/JsonValue、拡張メソッド、decimalの数値モデル、同名キーの後勝ちを維持します。キー・文字列値は正しくエスケープし、数値は現在のカルチャーに依存しません。文字列は解析時に一度だけJSONとして復号し、直接JsonValueへ渡した文字列にはRegex.Unescapeを適用しません。

ルートは従来どおりオブジェクトです。不正JSON、decimalの範囲外、64段を超えるネストはJsonExceptionで拒否します。JSONとして必要な変更のため、エスケープの見た目や不正入力に対する従来の偶発的例外型は互換対象外です。文字列の意味、キー数、公開ラッパーAPIを回帰テストで確認します。

.NET 8 SDKで `dotnet run --project tests/Rin.MyJson.Regression.csproj`。追加NuGetパッケージやネットワークサービスを使わない小規模テストです。新規projectはMyJson.csをライブラリとしてビルドし、既存Program.csは利用例として残します。従来の利用先のTFMは不明なので、.NET Framework等へ組み込む場合はSystem.Text.Jsonの対応バージョンを別途確認してください。

JsonArray.ToString()とJsonValueに包んだ配列も、同じ標準writerを経由した正当なJSONを返します。以前の文字列要素が未引用だった表示形式は修正対象です。既存Program.csの互換確認は `dotnet run --project examples/Rin.MyJson.Example.csproj` で行えます。
