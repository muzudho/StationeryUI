# dockLayout — 配列順に四辺を確保する

JSON の型名は `dock-layout`、開発者ウィンドウでの表示名は `dockLayout` です。`work-page-layout` は互換性のため残ります。

## 配置の規則

- `childrenModel` は空配列でもよく、要素数の上限はありません。同じ方向を複数指定できます。
- `top` / `bottom` の `size` は高さ、`left` / `right` の `size` は幅です。非負の `px` 文字列を指定します。`rate` は使いません。
- 四辺は配列順に**残っている領域**から確保します。先の要素が角を取り、後の要素はその分短くなります。
- `center` は最大１個で、`size` は `"remaining"`。配列のどこにあっても四辺を配置した最後に残り全部を使います。
- 指定サイズが残りより大きいときは、残りのサイズまで切り詰めます。領域が尽きた後は幅または高さが０になります。
- `center` がない場合、残りは空き領域です。binding に含めなかった親直下の子も、他の配置指定がなければ０サイズになります。
- 子は `parentModel` の直下のモデルを参照します。同じモデルの二重配置は禁止です。孫は子コンテナー側のレイアウトで配置します。

`row-definitions` / `column-definitions` はグリッド用です。ドックには置かず、各要素の `dock` / `size` を使います。モデルへの配置指定は既存のグリッドと同じく `bindings` に記述し、`layouts` には配置方式を定義します。

## 最小の設定例

上40px、下80px、本文が残りを使います。本文内にはグリッドを適用しています。

```json
{
  "models": [{
    "id": "app", "type": "viewport",
    "children": [
      { "id": "header", "type": "textBlock" },
      { "id": "inspector", "type": "container" },
      { "id": "body", "type": "container", "children": [
        { "id": "button", "type": "button" }
      ] }
    ]
  }],
  "layouts": [
    { "id": "frame", "type": "dock-layout" },
    { "id": "bodyGrid", "type": "grid-layout",
      "row-definitions": ["1rate"], "column-definitions": ["1rate"] }
  ],
  "bindings": [
    { "layout": "frame", "parentModel": "app", "childrenModel": [
      { "model": "header", "dock": "top", "size": "40px" },
      { "model": "inspector", "dock": "bottom", "size": "80px" },
      { "model": "body", "dock": "center", "size": "remaining" }
    ] },
    { "layout": "bodyGrid", "parentModel": "app/body", "childrenModel": [
      { "model": "button", "row": 0, "col": 0 }
    ] }
  ]
}
```

ドックと別のルートグリッドで同じ親の全域を取り合う指定はエラーです。中央の `body` のような子コンテナーへグリッドを適用してください。親の `box-layout` とドックを組み合わせると、その余白の内側にドックを配置できます。ドックを `box-layout` や `grid-layout` の子レイアウトとして定義することもできます。ドック自身の配置先は `bindings.childrenModel` で指定します。

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
