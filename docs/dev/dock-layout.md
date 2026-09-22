# dockLayout — 配列順に四辺を確保する

JSON の型名は `dock-layout`、開発者ウィンドウでの表示名は `dockLayout` です。`work-page-layout` は互換性のため残ります。



## 配置枠とモデルの対応

配置情報は `layouts` に集めます。grid-layout の `slots` に `id`・`row`・`col`・必要な span、dock-layout の `slots` に `id`・`dock`・`size` を書きます。`bindings.childrenModel` は `{"slot":"input", "model":"nameField"}` のような対応だけを持ちます。枠名とモデル Id は別物なので、一致させる必要はありません。

枠名は同じレイアウト内で一意です。異なるレイアウトでは同じ枠名を再利用できます。未知の枠、同じ枠への二重割り当て、同じモデルの二重配置はエラーです。グリッドの枠は、モデルを割り当てていなくても範囲外・重複・子レイアウトとの重なりを検証します。未割り当ての枠は空き領域として残り、ドックではそのサイズを確保します。ドックの配置順は slots の順で決まり、bindings の並べ替えでは変わりません。

旧形式の `bindings.childrenModel` にある行・列・span・dock・size は読み込みエラーになります。これらを参照先レイアウトの slots に移し、各枠へ Id を付け、binding 側は slot と model だけにしてください。共有していたレイアウトで配置が異なる場合は、レイアウト定義を分けます。デモでは下端80px用の pageDock と0px用の pageDockFullscreen を使います。

split-pane の firstModel / secondModel と旧ページレイアウトの inspectorModel は、組み込みの配置枠への対応です。方向・比率・高さなどの配置定義は従来どおり layouts にだけ書きます。１ノードが持つルートレイアウトは最大１つのままです。

## １ノード１レイアウトと共通の padding

各モデルノードが持てるルートレイアウトは最大１つです。`box-layout` / `grid-layout` / `dock-layout` はどれも `padding` を指定できます。例えば `"padding": {"top":"8px","right":"8px","bottom":"8px","left":"8px"}` とします。値は非負の px 文字列です。省略した辺は box-layout では従来どおり8px、grid-layout / dock-layout では0pxです。padding を引いた内側に子を配置し、領域が足りない場合は０サイズまで縮めます。

複合配置はレイアウトの `children` でネストします。同じルートツリー内の `/frame/grid` などへ複数 binding を書いても、所有するレイアウトは１つです。dock の各領域をさらに分割するときは子コンテナーに grid などを持たせます。別々のルートを同一ノードへ binding すると、読み込み時にエラーになります。実行中のリロードでは直前の有効な設定を維持します。

旧設定で余白用 box と配置用 grid を併用していた場合は、box の padding を grid へ移して box の binding を削除してください。margin / border も必要なら、box の `children` に grid を入れ、binding の layout を `/boxId/gridId` に変更します。

## 配置の規則

- `slots` は空配列でもよく、要素数の上限はありません。同じ方向を複数指定できます。
- `top` / `bottom` の `size` は高さ、`left` / `right` の `size` は幅です。非負の `px` 文字列を指定します。`rate` は使いません。
- 四辺は `layouts.slots` の配列順に**残っている領域**から確保します。先の要素が角を取り、後の要素はその分短くなります。
- `center` は最大１個で、`size` は `"remaining"`。配列のどこにあっても四辺を配置した最後に残り全部を使います。
- 指定サイズが残りより大きいときは、残りのサイズまで切り詰めます。領域が尽きた後は幅または高さが０になります。
- `center` がない場合、残りは空き領域です。binding に含めなかった親直下の子も、他の配置指定がなければ０サイズになります。
- 子は `parentModel` の直下のモデルを参照します。同じモデルの二重配置は禁止です。孫は子コンテナー側のレイアウトで配置します。

`row-definitions` / `column-definitions` はグリッド用です。ドックには置かず、各要素の `dock` / `size` を使います。配置枠は `layouts.slots` に `id`・`dock`・`size` を定義します。`bindings.childrenModel` は `slot` と `model` の対応だけを持ちます。空の枠も領域を確保し、binding の配列順は配置へ影響しません。

## 最小の設定例

上40px、下80px、本文が残りを使います。本文内にはグリッドを適用しています。

```json
{
    "models": [
        {
            "id": "app",
            "type": "viewport",
            "children": [
                {
                    "id": "header",
                    "type": "textBlock"
                },
                {
                    "id": "inspector",
                    "type": "container"
                },
                {
                    "id": "body",
                    "type": "container",
                    "children": [
                        {
                            "id": "button",
                            "type": "button"
                        }
                    ]
                }
            ]
        }
    ],
    "layouts": [
        {
            "id": "frame",
            "type": "dock-layout",
            "slots": [
                {
                    "id": "header",
                    "dock": "top",
                    "size": "40px"
                },
                {
                    "id": "inspector",
                    "dock": "bottom",
                    "size": "80px"
                },
                {
                    "id": "body",
                    "dock": "center",
                    "size": "remaining"
                }
            ]
        },
        {
            "id": "bodyGrid",
            "type": "grid-layout",
            "row-definitions": [
                "1rate"
            ],
            "column-definitions": [
                "1rate"
            ],
            "slots": [
                {
                    "id": "button",
                    "row": 0,
                    "col": 0
                }
            ]
        }
    ],
    "bindings": [
        {
            "layout": "/frame",
            "parentModel": "app",
            "childrenModel": [
                {
                    "model": "header",
                    "slot": "header"
                },
                {
                    "model": "inspector",
                    "slot": "inspector"
                },
                {
                    "model": "body",
                    "slot": "body"
                }
            ]
        },
        {
            "layout": "/bodyGrid",
            "parentModel": "app/body",
            "childrenModel": [
                {
                    "model": "button",
                    "slot": "button"
                }
            ]
        }
    ]
}
```

ドックと別のルートグリッドで同じ親の全域を取り合う指定はエラーです。中央の `body` のような子コンテナーへグリッドを適用してください。余白はドック自身の `padding` に指定できます。同じ親に別のルート `box-layout` を付けることはできません。ドックを `box-layout` や `grid-layout` の子レイアウトとして定義することもできます。ドック内の配置は `layouts.slots`、枠とモデルの対応は `bindings.childrenModel` で指定します。

`work-page-layout` から移行する場合は、本文コンテナーを作り、下端のインスペクターを `bottom`、本文を `center` にします。従来のページレイアウトとは同じ親に重ねません。既存ファイルを自動で書き換えることはありません。

## エラー時の表示と復旧

`StationeryStyleSettings.Parse(json)` は検証用の厳格な API で、無効な設定を `JsonException` として返します。エディターもこれを使うので、無効な配置を正常な設定として保存しません。

実行中の `StationeryStyleFile` は `ParseWithDockFallback(json)` を使います。ドックの親が読み取れる状態で、子の方向・サイズ・参照・center の重複などにエラーがあると、次の代替配置を返します。

1. 親の上端に最大64pxのエラー表示領域を確保します。
2. 親直下の子を **models の宣言順**に、１要素48pxで縦に並べます。幅は親の残り領域いっぱいです。
3. 収まりきらない要素は残りの高さで切り詰め、以降は０サイズにします。自動スクロールは追加しません。
4. 子コンテナー自身の有効なレイアウトは引き続き適用します。

これは診断用の一時的な表示です。元の JSON を書き換えません。設定を修正して再読み込みすると、通常のドック配置に戻ります。`LastError` は代替表示中もエラーを保持し、F12 の親ノードにもエラーの全文を表示します。

JSON の構文エラー、未知のレイアウト型、親の参照エラー、他の配置との競合など、親子関係や配置を安全に確定できない場合は、直前に読み込めた状態を維持します。初回読み込み失敗時はアプリから渡された fallback（なければライブラリーの既定設定）を使います。グリッドなど既存の配置エラーの扱いは変更しません。

## MonoGame ホストの接続

従来どおり `StationeryLayoutEngine.Arrange(settings, width, height)` の `Bounds` を各部品へ反映します。代替配置のエラーは `StationeryLayoutResult.Errors` に、親の完全パス・レイアウトパス・メッセージ・画面ピクセル単位の表示領域として入ります。

子の描画を終えた後に次を呼ぶと、親の予約領域にエラーを表示します。複数ページがある場合は `include` で表示中の親だけに絞ってください。

```csharp
ui.Draw();
ui.DrawLayoutErrors(layoutResult);
// F12 用のスナップショットにも、現在の設定とエラー情報を含める。
developerWindow.Update(ui.Inspect(settings));
```

`DrawLayoutErrors` は内部で SpriteBatch を開始・終了します。アプリ側の `SpriteBatch.End` を済ませてから呼びます。エラー全文は `Errors` と F12 の詳細欄で取得できます。画面上の見出しは予約領域でクリップされます。

スタイル設定エディターは既存ドック設定の読み込み・保存で `dock` / `size` を保持し、モデル名変更時の参照も更新します。ドック専用の追加・サイズ編集 UI はまだありません。JSON で設定してください。


## 配置枠を基準にした margin

box-layout / grid-layout / dock-layout と、grid / dock の slots に `margin` を指定できます。`{"left":"10px","top":"10px","right":"10px","bottom":"10px"}` の四辺形式で、非負の px 文字列を使い、省略した辺は0pxです。bindings へは書きません。

親の padding の内側を grid / dock で配置枠へ分け、その枠から margin を引きます。margin は隣の枠の位置や大きさを変えません。モデル自身のルートレイアウトにも margin があればさらに引き、次に padding を引いて子の配置領域を得ます。余白が大きすぎる場合、幅・高さは0まで縮みます。ネストした子レイアウトも自身の割り当て枠が基準です。

F12 の詳細欄には margin を外側、padding を内側とする Compound 図を表示します。数値は設定上の px で、帯の幅は模式図です。モデルの配置枠とルートレイアウトの margin は合計し、padding はそのモデルのルートレイアウトの値を表示します。祖先や内部の子レイアウトの余白は合算しません。スタイル情報を渡していない従来の検査データには図を表示しません。

デモ本文のグリッドは四辺 margin 4px + padding 4px とし、従来の合計8pxの余白を保ちます。レイアウトデモの戻るリンクの配置枠にも margin を設定しています。
