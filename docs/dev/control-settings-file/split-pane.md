# ページ遷移とスプリットペーン

起動時の画面は **トップデモページ（topDemoPage）**。
右下の下線付き「リンク：スプリットペーン」をクリックすると、
**スプリットペーンデモページ（splitPaneDemoPage）**へ移動する。
ページ上部の「← トップデモページに戻る」で戻れる。Tab でリンクを選び、Enter / Space でも移動できる。

ページを切り替えても、入力内容・ツリーの開閉状態・分割位置は保持する。
表示していないページは入力を受け取らず、F12 では非表示として確認できる。
トップページの名前欄のパスは /demo/topDemoPage/nameField。

## 二種類の分割

| orientation | 見た目 | 操作 |
|---|---|---|
| vertical | 垂直の仕切りで左右に分割 | 仕切りを左右へドラッグ |
| horizontal | 水平の仕切りで上下に分割 | 仕切りを上下へドラッグ |

同じページの上半分が垂直分割、下半分が水平分割のデモ。
四つのペーンには編集できるテキスト欄を置いている。
仕切りをドラッグ中はペーンの外に出ても追従し、ボタンを離すと止まる。
Tab で仕切りにフォーカスした場合は、左右分割なら ← / →、上下分割なら ↑ / ↓ で比率を変更できる。

## スタイル設定

App_Data/demo.stationery-style.json の models / layouts / bindings を使う。
models には demo の下に二つの page を置き、splitPane の下には二つの子を置く。
ページとスプリットペーン自体の配置は grid-layout が担当する。
ペーン内部の配置は次の split-pane 定義が担当する。

```json
{
    "id": "verticalSplitLayout",
    "type": "split-pane",
    "orientation": "vertical",
    "ratio": 0.5,
    "dividerWidth": "10px",
    "minimumPaneSize": "80px"
}
```

- orientation は必須で vertical / horizontal のどちらか。
- ratio は最初のペーンの比率。0～1 の数値で、既定値は 0.5。仕切り幅を除いた残りを配分する。
- dividerWidth は正の px 文字列。既定値は 8px。
- minimumPaneSize は各ペーンの最小幅または最小高さ。非負の px 文字列で、既定値は 40px。

画面が最小サイズの合計より狭いときは、残りを半分ずつ分ける。仕切り自体も収まらない場合は仕切りを縮める。
px は画面上のピクセルで、表示倍率によって仕切りが太くなることはない。

対応付けは次の形式。

```json
{
    "layout": "/verticalSplitLayout",
    "model": "demo/splitPaneDemoPage/verticalSplit",
    "firstModel": "leftPane",
    "secondModel": "rightPane"
}
```

model は splitPane 型。firstModel / secondModel は、その直下の別々の子を参照する。
相対パスの基準は model。完全パスも使える。firstModel が左または上、secondModel が右または下。
同じ子の二重配置、存在しない参照、範囲外の比率は読み込みエラーになる。

保存時のオートリロード／F5 に対応する。不正な設定なら最後の正常な設定を保つ。
分割設定が変わると ratio を初期値として適用し直す。分割設定が同じなら、ドラッグで変更した比率を保つ。
ドラッグ結果は JSON に書き戻さない。
