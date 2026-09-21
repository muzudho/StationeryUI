# オートリロードとファイルの配置・配布

[目次](README.md)

## オートリロードの設定

`StationeryStyleFile` に渡すのは、スタイル JSON ではなく次の **読み込み設定 JSON のパス**です。

```json
{
    "styleFile": "app.stationery-style.json",
    "autoReload": true
}
```

styleFile の相対パスは、この読み込み設定ファイルのあるフォルダーが基準です。絶対パスも使えます。省略すると `demo.stationery-style.json` になるため、別アプリでは明示指定を勧めます。autoReload は真偽値で、省略時 true です。

**読み込み設定ファイルは常に監視**します。autoReload が制御するのはスタイルファイルの監視だけです。false にしても、読み込み設定を true に変更すれば再開できます。スタイル JSON 内の autoReload は監視方針になりません。

| 操作 | 動作 |
| --- | --- |
| 起動する | コンストラクター内で両ファイルを読み込む。autoReload=false でも初回は読む |
| true のままスタイルを保存する | 安定した内容を確認して Current を更新する |
| false のままスタイルを保存する | 現在の設定を維持する |
| false → true にして読み込み設定を保存する | 監視再開後、停止中の変更も読む |
| styleFile を別パスへ変更する | autoReload=false でも新しいファイルを一度読む。失敗なら再試行する |
| `styles.Reload()` を呼ぶ | autoReload にかかわらず両ファイルをすぐ読む |

`styles.Update(gameTime.ElapsedGameTime)` を毎フレーム呼びます。約500ms間隔で調べ、同じ変更内容を2回読めたら採用します。通常の変更は約0.5～1秒、監視再開やパス変更では約1～1.5秒が目安です。Update を止めた間は監視も進みません。別スレッドの FileSystemWatcher ではありません。

Update / Reload の戻り値は **新しいスタイルを採用した場合に true** です。false はエラーとは限りません。未変更や監視待ちでも false になります。エラーは `LastError` を確認します。`Current` の参照が変わったかで適用を管理してもかまいません。リサイズによる矩形計算は、スタイル変更の有無とは別に必要です。

F5 への割り当てはアプリの仕事です。ライブラリーがキーを自動監視するわけではありません。[最小例](integration.md)では押した瞬間だけ Reload を呼びます。

## アプリ固有の検証と失敗時の動作

パーサーは JSON 形式・参照先などを検証しますが、「このアプリには保存ボタンが必須」という情報は知りません。`validate` に、必要な Id・型・セルへの接続・許可する階層を確認する処理を渡してください。不正なら `JsonException` または `ArgumentException` を送出します。

検証は副作用のない処理にします。UI を作り直したり、編集中のテキストを消したりせず、新しい設定が使用可能かだけを判定します。パーサーの検証に加えて `StationeryLayoutEngine.Arrange` を試すと、アプリが使うレイアウトの組み合わせも確認できます。

- スタイルの読取・解析・検証失敗：最後に採用した Current を維持し、LastError に情報を入れます。
- 読み込み設定の失敗：最後のパスと監視方針を維持します。起動時は既定の読み込み方針を使います。
- 初回スタイルの失敗：渡した fallback を使い、初回成功まで再試行します。fallback の省略時は `demo` ルートだけの標準設定なので、別アプリの部品には通常不足します。
- **fallback 自体の validate 失敗はコンストラクターから例外が出ます。** 正常な既定設定を用意してください。
- validate の任意の例外をすべて握りつぶすわけではありません。実装バグの `NullReferenceException` などは呼び出し元へ出ます。

LastError が変わったときにログ・ステータス・インスペクターへ表示すると、最後の正常な画面を見て「リロードに成功した」と誤解しにくくなります。エラー表示の場所はアプリが決めます。ウィンドウタイトルへの表示はデモの実装です。

## 開発時と配布時のパス

基本形は `Path.Combine(AppContext.BaseDirectory, "App_Data", "app.stationery-config.json")` です。カレントディレクトリに依存しないため、ショートカットや他の場所から起動しても同じ設定を読みます。[コピー設定](integration.md)で build と publish の両方へ含めてください。

この方法で実行中に編集するのは **出力先のコピー**です。ソース側の App_Data を編集して即反映したい開発用アプリでは、アプリ自身で明示的なパス切り替えを実装します。たとえば環境変数を読むなら以下の形です。

```csharp
var configured = Environment.GetEnvironmentVariable("MYTOOL_STATIONERY_CONFIG_PATH");
var configurationPath = string.IsNullOrWhiteSpace(configured)
    ? Path.Combine(AppContext.BaseDirectory, "App_Data", "app.stationery-config.json")
    : Path.GetFullPath(configured);
```

相対指定の環境変数値はカレントディレクトリが基準になるので、運用では絶対パスを指定してください。デモの `STATIONERYUI_CONFIG_PATH` や Debug 用 AssemblyMetadata の処理は **デモ側の実装**です。別アプリで StationeryStyleFile を作るだけでは自動で働きません。

インストール先が書き込み不可、またはアップデートで置き換わる場所なら、編集用の設定はユーザーの書き込み可能なデータフォルダーへ初回だけコピーして、そのパスを渡します。再起動のたびに原本で上書きしないでください。StationeryStyleFile は読み取り専用の機能で、保存先の作成やコピーは行いません。

## 埋め込みの既定設定

固定の既定値は、[最小例](integration.md)の raw string から Parse して fallback に渡せます。大きい JSON はアプリの `.csproj` に埋め込みリソースとして登録する方法もあります。

```xml
<ItemGroup>
  <EmbeddedResource Include="Defaults/app.stationery-style.json"
                    LogicalName="MyTool.DefaultStyle.json" />
</ItemGroup>
```

```csharp
using var stream = typeof(StyledGame).Assembly
    .GetManifestResourceStream("MyTool.DefaultStyle.json")
    ?? throw new InvalidOperationException("Missing default style resource.");
using var reader = new StreamReader(stream);
var fallback = StationeryStyleSettings.Parse(reader.ReadToEnd());
var styles = new StationeryStyleFile(configurationPath, fallback, StyledGame.ValidateStyle);
```

このコードの StyledGame は最小例のクラスで、実際にはリソースを埋め込んだアプリの型を指定します。埋め込み設定自体は実行中に編集できません。外部ファイルを一切使わない場合は、Parse の結果を直接 Arrange へ渡し、StationeryStyleFile を作らない構成にできます。

[開発者ウィンドウの設定](../../../src/StationeryUI.MonoGame/StationeryDeveloperStyle.cs)は埋め込みの実例ですが、専用の window 設定や起動時だけの読み込みを持ちます。一般のスタイル監視機能と同一視しないでください。

## オートセーブとの違い

オートリロードは **利用アプリが JSON の変更を読む機能**です。エディターのオートセーブ・`.N.bak`・セーブポイント復元は、**エディターが JSON を書く機能**です。StationeryStyleFile を使っても自分のアプリの入力データや JSON が自動保存されるわけではありません。
