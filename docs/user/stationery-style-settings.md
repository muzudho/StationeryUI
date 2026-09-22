# 文房具ＵＩのスタイル設定ファイルのユーザーズガイド

この文書は、デモアプリのスタイル設定を編集する方向けの仕様です。アプリごとに編集可能な項目は異なります。

配置対象の文房具の Id とパスは、[F12 開発者ウィンドウ](developer-window.md) で確認できる。
bindings でこのパスを使い、モデルとレイアウトを結び付ける。

## 設定ファイルの置き場所

設定は次の 2 ファイルに分ける。どちらも JSON 形式。

| ファイル | 内容 |
|---|---|
| `App_Data/demo.stationery-config.json` | スタイルの読み込み先とオートリロード設定 |
| `App_Data/demo.stationery-style.json` | models の文房具構造、layouts の配置設定、bindings の対応付け |

配布されたデモでは、実行ファイル横の `App_Data` にある設定を編集します。

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
モデルの type: viewport は継続するが、レイアウトの型は box-layout / grid-layout / split-pane を使う。

以下は形式を説明する小さな例。実際のデモでは原本の二つのページと、その中の文房具も必要。

```json
{
    "models": [
        {
            "id": "demo",
            "type": "viewport",
            "children": [
                {
                    "id": "nameField",
                    "type": "textBox"
                },
                {
                    "id": "memoField",
                    "type": "textBox"
                }
            ]
        }
    ],
    "layouts": [
        {
            "id": "demoPage",
            "type": "grid-layout",
            "row-definitions": [
                "1rate"
            ],
            "column-definitions": [
                "1rate",
                "1.5rate"
            ],
            "slots": [
                {
                    "id": "nameField",
                    "row": 0,
                    "col": 0
                },
                {
                    "id": "memoField",
                    "row": 0,
                    "col": 1
                }
            ],
            "padding": {
                "top": "32px",
                "right": "32px",
                "bottom": "8px",
                "left": "32px"
            }
        }
    ],
    "bindings": [
        {
            "layout": "/demoPage",
            "parentModel": "demo",
            "childrenModel": [
                {
                    "model": "nameField",
                    "slot": "nameField"
                },
                {
                    "model": "memoField",
                    "slot": "memoField"
                }
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
レイアウト Id も英字・数字・アンダースコアを使い、同じ親の下で一意にする。
box-layout は children に0～1個、grid-layout は0～複数個の子レイアウトを置ける。モデル参照や contents は置かない。
bindings.layout には先頭 `/` 付きのスラッシュ区切りの完全パスを指定する。子の row・col・rowspan・colspan の指定例は[レイアウトをネストする](nested-layouts.md)を参照。

## bindings の参照と組み合わせ

box-layout には layout と model を指定する。
grid-layout には layout、parentModel、childrenModel を指定する。
childrenModel の各要素は slot と model の対応だけを持つ。slot は参照先レイアウトの slots の Id、model は parentModel からたどる相対パス。行・列・span は layouts.slots に定義する。

- parentModel: demo、model: nameField → /demo/nameField。
- parentModel: demo、model: inputs/nameField → /demo/inputs/nameField。
- /demo/editDialog/nameField のような完全パスも使用できる。
- トップレベルの model / parentModel は demo または /demo のようにルートから指定する。
- 大文字と小文字を区別する。短い Id の全体検索、ワイルドカード、.. は使わない。

配置対象は parentModel の子孫に限る。親自身や別の枝には配置できない。
parentModel の型は viewport / page / container / dialog。
存在しないモデルやレイアウトへの参照は読み込みエラー。

同じモデルに適用できるルートレイアウトは最大１つ。box-layout / grid-layout / dock-layout はどれも padding を持てる。余白だけなら grid / dock 自身に指定し、細かい配置は children または子コンテナーでネストする。
padding は四辺の非負 px 文字列で、省略した辺は box では従来どおり8px、grid / dock では0px。
独立した複数のルートグリッドやルートボックスを同じモデルに重ねて指定することはできない。同じレイアウトツリー内の複数グリッドを参照する場合は、同じ parentModel を使える。
別のモデルであれば、同じレイアウト定義を再利用できる。

配置したコンテナーにさらにレイアウトを結び付けることで、入れ子の配置もできる。
layouts / bindings の配列順には依存せず、外側のモデルから内側へ計算する。ただしドックの角の優先順は layouts.slots の配列順で決まる。
セルに配置していないモデルは親の内側領域を引き継ぐ。
モデルの階層を変更したら bindings のパスも更新する。レイアウトの変更だけでは文房具のパスは変わらない。

## grid-layout と rate

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

## box-layout のパディング

box-layout には `margin` と `border` も指定できる。いずれも `padding` と同じ四辺のオブジェクト形式で、非負の px 文字列を使う。margin / border の省略した辺は 0px。

```json
{
    "id": "contentPanel",
    "type": "box-layout",
    "margin": { "top": "10px", "right": "10px", "bottom": "10px", "left": "10px" },
    "padding": { "top": "8px", "right": "8px", "bottom": "8px", "left": "8px" },
    "border": { "top": "2px", "right": "2px", "bottom": "2px", "left": "2px" }
}
```

割り当て領域から margin を引いたものがパネルの外枠 `Bounds`、そこから padding を引いたものが `ContentBounds`。`border` は **全体のサイズ計算に含めない**。外枠の外側へ広がる描画領域を `BorderBounds` に返し、子の配置やグリッドのサイズを変えない。余白が足りない場合、枠は隣の領域へ重なる可能性がある。ウィンドウ外は描画時に切り取る。

例えば幅 100px、左右 margin が各 10px、左右 padding が各 8px なら外枠は 80px、内容は 64px。左右 border を各 2px にしても外枠と内容の幅は変わらず、枠の描画幅だけが 84px になる。

padding の四辺を "0px"、"8px"、"12.5px" のような非負の px 文字列で指定する。
省略した辺は 8px。数値だけの 8、負数、rate、%、em、auto は未対応。
box-layout のパディングは子を配置する領域を狭める設定で、各コントロール内部の文字余白とは別。

px は MonoGame の描画領域のピクセル単位。ウィンドウ枠を含まない。
左・上のパディングを原点とし、右・下も差し引いた内側を使う。
パディングが大きすぎる場合、内側の幅・高さは 0 まで縮める。
デモではルートの内側が 1px 未満になると、コンテンツの描画・操作を休止する。
F5 と自動リロードは継続する。

デモの拡大率ボタンは、セルの位置・大きさを保ったまま文字などの表示倍率を変える。
パディングやセルの比率は変わらない。ダイアログは内側の領域に収まる倍率で中央に配置する。

## ページの表示領域とインスペクターパネル

フルスクリーンはアプリのウィンドウ内いっぱいに表示する意味で、OS の全画面モードには切り替えない。

| 種類 | 表示 |
|---|---|
| `dock-layout` の bottom が `0px` | 本文がページ全体を使い、インスペクターは非表示 |
| `dock-layout` の bottom が `80px` | 本文の下に高さ80pxのインスペクターを確保 |

デモのトップとレイアウトページは `pageDock`、スプリットページは `pageDockFullscreen` を使う。トップとレイアウトデモの下端は80px、スプリットペーンデモは0px。原本は `App_Data/demo.stationery-style.json`。

関連部分の抜粋：

```json
{
    "layouts": [
        {
            "id": "pageDock",
            "type": "dock-layout",
            "slots": [
                {
                    "id": "inspectorPanel",
                    "dock": "bottom",
                    "size": "80px"
                },
                {
                    "id": "body",
                    "dock": "center",
                    "size": "remaining"
                }
            ]
        }
    ],
    "bindings": [
        {
            "layout": "/pageDock",
            "parentModel": "demo/topDemoPage",
            "childrenModel": [
                {
                    "model": "inspectorPanel",
                    "slot": "inspectorPanel"
                },
                {
                    "model": "body",
                    "slot": "body"
                }
            ]
        }
    ]
}
```

layouts.slots にある bottom の `size` を `0px` に変えると、下端を非表示にして本文へ領域を戻せる。`models` や本文のセル配置は同じまま使う。四辺は配列順に確保し、center は最後に残りを使う。
読み込み用設定の `autoReload` が有効なら保存後に反映され、文房具 Id や入力値、ツリーの開閉状態を保持する。

ページ直下の `body` と `inspectorPanel` は container。デモは `inspectorPanel` 内に読み取り専用の `toolHint` を置き、grid-layout の binding でパネル全体へ広げている。
ボタンやリンクにマウスを合わせると説明が表示され、離すと案内文に戻る。この下部パネルは F12 の別ウィンドウとは独立している。

高さの px は UI 拡大率に影響されない。ページが 80px より低い場合は収まる高さに縮める。
インスペクターはページの外枠いっぱいの横幅を使い、ページ本文の padding の影響を受けない。祖先の padding でページ全体が狭められている場合は、その幅に収まる。
デモではルートの padding を 0px にし、本文の8pxの余白を body の grid-layout 自身の `padding` に指定する。これでパネルは画面の左右端まで広がる。

旧 `fullscreen-layout` / `work-page-layout` も互換用として利用できるが、現在のデモでは使わない。[ドック配置の詳しい仕様](../dev/dock-layout.md)を参照。
