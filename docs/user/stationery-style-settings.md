# 文房具ＵＩのスタイル設定ファイルのユーザーズガイド

別のアプリへ組み込む場合は、[スタイル設定のプログラミングガイド](style-settings/README.md)を参照してください。以下は主にこのリポジトリーのデモでの利用仕様です。

配置対象の文房具の Id とパスは、[F12 開発者ウィンドウ](developer-window.md) で確認できる。
bindings でこのパスを使い、モデルとレイアウトを結び付ける。

## 設定ファイルの置き場所

設定は次の 2 ファイルに分ける。どちらも JSON 形式。

| ファイル | 内容 |
|---|---|
| `App_Data/demo.stationery-config.json` | スタイルの読み込み先とオートリロード設定 |
| `App_Data/demo.stationery-style.json` | models の文房具構造、layouts の配置設定、bindings の対応付け |

開発時（Debug ビルド）はリポジトリーの `App_Data` の原本を使う。
ビルド／publish 時には両方を出力先の `App_Data` にコピーする。
配布時（Release ビルド）は実行ファイル横の `App_Data/demo.stationery-config.json` を使うので、配布物に両ファイルを含める。
Debug 版でも原本のフォルダーが存在しない場合は実行ファイル横のコピーを使う。

環境変数 `STATIONERYUI_CONFIG_PATH` を指定すると、別の読み込み設定ファイルを使える。絶対パスを推奨する。
以前の `STATIONERYUI_STYLE_PATH` は廃止。スタイルの読み込み先は次の `styleFile` に指定する。
通常の読み込み先はカレントディレクトリに依存しない。
Release 版で編集するのは出力先のコピー。再ビルドや再配布で更新されることがあるため、独自設定を保持したい場合は環境変数で別の設定ファイルを指定する。

## 読み込み先とオートリロード

`demo.stationery-config.json` の例：

```json
{
    "styleFile": "demo.stationery-style.json",
    "autoReload": true
}
```

`styleFile` は、この設定ファイルのあるフォルダーを基準とする相対パス、または絶対パス。
省略時は `demo.stationery-style.json`。空文字や文字列以外はエラー。
`autoReload` は `true` / `false` の真偽値で、省略時は `true`。

**読み込み設定ファイルは常に監視する。** `autoReload` が制御するのはスタイルファイルの変更監視だけ。

- `true`：スタイルファイルの保存を検知して反映する。
- `false`：スタイルの現在の表示を維持し、スタイルファイルの変更を自動反映しない。
- `false` から `true`：読み込み設定を保存するだけで監視が再開し、停止中のスタイル変更も反映する。F5 や再起動は不要。
- `styleFile` の変更：新しいファイルを読み込む。`autoReload: false` でも、読み込み先の切り替え時には一度読み込む。

約 0.5 秒ごとに確認し、同じ変更内容を 2 回続けて読めたら採用する。
通常のスタイル変更は約 0.5～1 秒、監視の再開やパスの切り替えからスタイル反映までは約 1～1.5 秒が目安。
デモ画面で **F5** を押すと、自動反映の有効／無効にかかわらず両ファイルをすぐに読み直す。
起動時にも両ファイルを読み込む。リロードしても入力中のテキストや選択中のテーマは維持する。

JSON の構文エラー、不正な値、ファイルの削除や読み取り失敗があっても、それぞれ最後に正常に読めた設定を維持する。
読み込み設定のエラー中は、最後に正常だったパスと監視方針を使う。
切り替え先が読めないときは現在の見た目を維持し、読み込みに成功するまで再試行する。
起動時に設定が読めない場合は、既定のパスとオートリロード有効で動作する。スタイルも読めない場合は四辺 `8px` で表示し、初回読み込み成功まで再試行する。
失敗はウィンドウタイトルに表示し、詳細と起動時の読み込み先はデバッガーの出力に記録する。

スタイルファイル自体には `autoReload` を置かない。以前の場所に残っている場合は、未知のプロパティとして無視される。

## models・layouts・bindings

トップレベルには 3 つの配列を置く。すべて必須。

| 配列 | 役割 |
|---|---|
| models | 文房具の所属・親子関係。現在は viewport 型のルートを 1 要素置く |
| layouts | モデルから独立したレイアウト定義。独自の Id を付ける |
| bindings | どのモデルに、どのレイアウトを適用するかの対応付け |

旧トップレベルの viewport / model / layout は廃止。旧形式との混在もエラー。
モデルの type: viewport は継続するが、レイアウトの型は panel / floating-layout / split-pane を使う。

以下は形式を説明する小さな例。実際のデモでは原本の二つのページと、その中の文房具も必要。

```json
{
    "models": [
        {
            "id": "demo",
            "type": "viewport",
            "children": [
                { "id": "nameField", "type": "textBox" },
                { "id": "memoField", "type": "textBox" }
            ]
        }
    ],
    "layouts": [
        {
            "id": "demoViewport",
            "type": "panel",
            "padding": {
                "top": "32px",
                "right": "32px",
                "bottom": "8px",
                "left": "32px"
            }
        },
        {
            "id": "demoPage",
            "type": "floating-layout",
            "row-definitions": ["1rate"],
            "column-definitions": ["1rate", "1.5rate"]
        }
    ],
    "bindings": [
        { "layout": "demoViewport", "model": "demo" },
        {
            "layout": "demoPage",
            "parentModel": "demo",
            "childrenModel": [
                { "model": "nameField", "row": 0, "column": 0 },
                { "model": "memoField", "row": 0, "column": 1 }
            ]
        }
    ]
}
```

models の各ノードには id と type、必要なら children 配列を置く。
文房具 Id は英字・数字・アンダースコアだけで構成し、camelCase を推奨。同じ親の直下では重複できない。
異なる親の下では同名にでき、外側からたどるパスで区別する。
詳しくは [文房具 Id の規約](developer-window.md) を参照。

layouts の id はモデルへの参照ではない。例えば demoViewport はレイアウト自身の名前で、
bindings の model: demo によって初めてモデルと結び付く。
レイアウト Id も英字・数字・アンダースコアを使い、layouts 配列内で一意にする。
layouts 内にモデル参照や children / contents は置かない。

## bindings の参照と組み合わせ

panel には layout と model を指定する。
floating-layout には layout、parentModel、childrenModel を指定する。
childrenModel の各要素の model は、parentModel からたどる相対パス。

- parentModel: demo、model: nameField → /demo/nameField。
- parentModel: demo、model: inputs/nameField → /demo/inputs/nameField。
- /demo/editDialog/nameField のような完全パスも使用できる。
- トップレベルの model / parentModel は demo または /demo のようにルートから指定する。
- 大文字と小文字を区別する。短い Id の全体検索、ワイルドカード、.. は使わない。

配置対象は parentModel の子孫に限る。親自身や別の枝には配置できない。
parentModel の型は viewport / page / container / dialog。
存在しないモデルやレイアウトへの参照は読み込みエラー。

同じモデルに panel と floating-layout をそれぞれ 1 つ適用できる。
panel が内側の領域を作り、floating-layout がその領域を行と列に分ける。
同じ型のレイアウトを同じモデルに重ねて指定することはできない。
別のモデルであれば、同じレイアウト定義を再利用できる。

配置したコンテナーにさらにレイアウトを結び付けることで、入れ子の配置もできる。
layouts / bindings の配列順には依存せず、外側のモデルから内側へ計算する。
セルに配置していないモデルは親の内側領域を引き継ぐ。
モデルの階層を変更したら bindings のパスも更新する。レイアウトの変更だけでは文房具のパスは変わらない。

## floating-layout と rate

row-definitions が上から下の行、column-definitions が左から右の列を定義する。
row / column は **0 始まり**。各セルに結び付いた文房具は、そのセル全体を占める。
行の中に文房具が 1 個だけでも、隣の空きセルへ自動的には広がらない。

rate は残りの領域を配分する比率で、小数も指定できる。
例えば ["1rate", "1.5rate"] は 2:3 の比率で、幅 500px なら 200px と 300px。
両方を同じ倍率に変えても配置結果は変わらない。
ウィンドウをリサイズすると、現在の描画領域に合わせて再計算する。

px との混在にも対応する。例えば ["120px", "1rate", "2rate"] は、
先に 120px を確保し、残りを 1:2 に分ける。
固定 px の合計が利用可能な領域を超えた場合は、固定部分を比例縮小し、rate 部分は 0px になる。
正の rate がなく、固定 px だけでは領域を埋めない場合は、末尾に空白を残す。

各定義は非負の有限な数を含む文字列にする。例：0px、12.5px、0rate、1.5rate。
負数、数値だけの値、指数表記、%、auto は未対応。
行・列の配列は空にできず、それぞれ最低 1 つは正の値が必要。
0rate の行・列は折り畳まれ、その中の部品は描画・操作の対象にならない。

範囲外の行・列、同じセルへの二重配置、同じ文房具の二重配置はエラー。
セル間の gap、複数セルにまたがる span、自動折り返し、内容に応じた行の高さは未対応。
CSS の float や Grid の互換実装ではなく、この JSON で指定した行・列と対応付けを使う独自の配置方式。

原本のトップデモページは 5 行 × 2 列。themeButton と scaleButton が同じ行の左右に並び、
名前欄の右側には sampleTree、右下には splitPaneDemoLink を配置する。メモ欄などの右側は空きセルになる。
ページ間の移動ともう一つのデモは [スプリットペーン](split-pane.md) を参照。

## panel のパディング

panel には `margin` と `border` も指定できる。いずれも `padding` と同じ四辺のオブジェクト形式で、非負の px 文字列を使う。margin / border の省略した辺は 0px。

```json
{
    "id": "contentPanel",
    "type": "panel",
    "margin": { "top": "10px", "right": "10px", "bottom": "10px", "left": "10px" },
    "padding": { "top": "8px", "right": "8px", "bottom": "8px", "left": "8px" },
    "border": { "top": "2px", "right": "2px", "bottom": "2px", "left": "2px" }
}
```

割り当て領域から margin を引いたものがパネルの外枠 `Bounds`、そこから padding を引いたものが `ContentBounds`。`border` は **全体のサイズ計算に含めない**。外枠の外側へ広がる描画領域を `BorderBounds` に返し、子の配置やグリッドのサイズを変えない。余白が足りない場合、枠は隣の領域へ重なる可能性がある。ウィンドウ外は描画時に切り取る。

例えば幅 100px、左右 margin が各 10px、左右 padding が各 8px なら外枠は 80px、内容は 64px。左右 border を各 2px にしても外枠と内容の幅は変わらず、枠の描画幅だけが 84px になる。

padding の四辺を "0px"、"8px"、"12.5px" のような非負の px 文字列で指定する。
省略した辺は 8px。数値だけの 8、負数、rate、%、em、auto は未対応。
panel のパディングは子を配置する領域を狭める設定で、各コントロール内部の文字余白とは別。

px は MonoGame の描画領域のピクセル単位。ウィンドウ枠を含まない。
左・上のパディングを原点とし、右・下も差し引いた内側を使う。
パディングが大きすぎる場合、内側の幅・高さは 0 まで縮める。
デモではルートの内側が 1px 未満になると、コンテンツの描画・操作を休止する。
F5 と自動リロードは継続する。

デモの拡大率ボタンは、セルの位置・大きさを保ったまま文字などの表示倍率を変える。
パディングやセルの比率は変わらない。ダイアログは内側の領域に収まる倍率で中央に配置する。

## デモの文房具とコードの役割

原本の models は、demo ルートの下に topDemoPage と splitPaneDemoPage を持つ。
topDemoPage には 8 部品と editDialog を定義する。
Id はコードの動作との接続にも使う固定名で、AI コーディング時に models と C# を合わせて生成・保守する。
表示文字列、入力処理、保存などの動作は C# が担当する。
JSON の type だけから任意の新しいコントロールを生成するわけではない。

メインには nameField / memoField（textBox）、
themeButton / scaleButton / applyTitleButton / openDialogButton（button）、sampleTree（tree）、splitPaneDemoLink（link）が各 1 個必要。
**トップページの 8 部品すべてに floating-layout のセルへの binding が必要。**
splitPaneDemoPage には topDemoLink、verticalSplit、horizontalSplit と、それぞれの子のテキスト欄を定義する。
スプリットペーンの子は split-pane の firstModel / secondModel で配置する。
ツリーの項目と開閉操作は [ツリーの使い方](tree.md) を参照。
ダイアログには nameField（textBox）、cancelButton / saveButton（button）が各 1 個必要。
ダイアログの配置は現時点では C# が担当するため、ダイアログやその子への binding はデモではエラーにする。

各範囲内で必要な部品を page / container で包み直せる。
欠落・重複・型違い・未接続の部品や不正な binding がある場合、設定全体を採用せず最後の正常な状態を維持する。
起動時に不正なスタイルだった場合は、コード内の標準モデル、5 行 × 2 列、四辺 8px を使う。

models / layouts / bindings の変更はすべて自動リロード／F5 の対象。
階層や配置を変更しても、既存コントロールのテキスト・選択範囲・テーマ・イベント処理は維持する。
モデルを変更するとフォーカスのパスも更新し、マウスキャプチャは解除する。
F12 は models のツリーと実際の文房具の配置座標を表示する。レイアウト専用ツリーの表示は未対応。

未知のプロパティは原則無視するため、設定名は上記の綴りに合わせる。

## 他の MonoGame アプリへの組み込み

StationeryUI.Styling の StationeryStyleFile に、スタイル自体ではなく読み込み設定ファイルのパスを渡す。
Update からファイルの Update を呼び、Current と現在の描画領域を配置エンジンに渡す。

```csharp
styles.Update(gameTime.ElapsedGameTime);
var arranged = StationeryLayoutEngine.Arrange(
    styles.Current, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
var nameBounds = arranged.Bounds["/demo/topDemoPage/nameField"];
var innerBounds = arranged.ContentBounds["/demo"];
```

Bounds は margin を差し引いたモデルごとの外枠、ContentBounds はさらに panel のパディングを差し引いた内側。BorderBounds は panel の border が外へ広がる描画領域。いずれも完全パスをキーとする。
結果はウィンドウのピクセル座標。StationeryUiHost.Viewport に倍率・オフセットを設定している場合は、論理座標へ変換してから Element.Bounds に渡す。

Current.Models[0].CreateTree() でノードを作り、StationeryUiHost.AddTextBox / AddButton のノード指定版で結び付ける。
モデルの更新には RebindModel を使う。任意の独自アプリでは、配置対象とコードの役割の検証も行う。
StationeryStyleFile の validate コールバックに検証を渡すと、不正な設定の採用を防げる。

Configuration は監視方針、ConfigurationFilePath と FilePath はそれぞれの読み込み先。
layouts の配列順とモデルは独立しているため、Layouts[0] から対象モデルの設定を決めない。

## ページの表示領域とインスペクターパネル

フルスクリーンはアプリのウィンドウ内いっぱいに表示する意味で、OS の全画面モードには切り替えない。

| 種類 | 表示 |
|---|---|
| `fullscreen-layout` | 本文がページ全体を使い、インスペクターは高さ 0 で非表示 |
| `work-page-layout` | 本文の下にインスペクターを確保。既定の高さは 80px |

デモはトップデモページを作業ページ、スプリットペーンデモページをフルスクリーンにしている。原本は `App_Data/demo.stationery-style.json`。

関連部分の抜粋：

```json
{
    "layouts": [
        { "id": "fullscreenLayout", "type": "fullscreen-layout" },
        { "id": "workPageLayout", "type": "work-page-layout", "inspectorHeight": "80px" }
    ],
    "bindings": [
        {
            "layout": "workPageLayout",
            "model": "demo/topDemoPage",
            "inspectorModel": "inspectorPanel"
        }
    ]
}
```

既存の binding の `layout` を `fullscreenLayout` に変えるだけで切り替えられる。`models`、本文のセル配置、`inspectorModel` は同じまま使う。同じページへ両方を同時に bind しない。
読み込み用設定の `autoReload` が有効なら保存後に反映され、文房具 Id や入力値、ツリーの開閉状態を保持する。

`inspectorModel` はページ直下の `container` を指定する。デモは `inspectorPanel` 内に読み取り専用の `toolHint` を置き、floating-layout の binding でパネル全体へ広げている。
ボタンやリンクにマウスを合わせると説明が表示され、離すと案内文に戻る。この下部パネルは F12 の別ウィンドウとは独立している。

高さの px は UI 拡大率に影響されない。ページが 80px より低い場合は収まる高さに縮める。
インスペクターはページの外枠いっぱいの横幅を使い、ページ本文の padding の影響を受けない。祖先の padding でページ全体が狭められている場合は、その幅に収まる。
デモではルートの padding を 0px にし、本文の 8px の余白を各ページの `pagePadding` に移した。これでパネルは画面の左右端まで広がる。

既存のデモ用 JSON を移行するときは、両ページへ inspectorPanel と toolHint を追加し、それぞれのページレイアウトとヒントのセルを bind する。原本と C# のフォールバックは同じ構造を保つ。
