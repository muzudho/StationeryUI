# F12 開発者ウィンドウ

デモ画面がアクティブな状態で **F12** を押すと、別ウィンドウに文房具の階層が開く。
Debug／Release の両方で使える。既に開いている場合は同じウィンドウを前面に出す。
開発者ウィンドウ内の **F12**、**Esc**、または閉じるボタンで閉じる。デモへ戻って F12 を押せば再度開ける。
デモ終了時は開発者ウィンドウも終了する。

ツリーから文房具を選ぶと、次の情報を確認できる。

- 文房具 Id と完全パス
- 種類と名前
- 表示中／非表示
- ウィンドウ内の位置と大きさ（px）

「パスをコピー」で完全パスをコピーできる。情報は実行中に更新される。
未表示のダイアログも「非表示」として表示する。ツリーの選択はデモ側の操作や入力フォーカスを変更しない。
表示状態は表示の有無を示すもので、モーダルによる操作可否や、他ウィンドウによる遮蔽は含まない。

## 文房具 Id の命名

文房具 Id はコードの動作と結び付く固定の名前。デモではスタイルファイルの `model` に階層とともに宣言する。
AI コーディング時は `model` の宣言と C# の接続箇所を合わせて生成・保守する。人間が設定ファイルや開発者ウィンドウから Id を登録・編集する必要はない。
実行時の連番やランダム値は使わず、再起動やスタイル変更で名前が変わらないようにする。

- 許可する文字は ASCII の `A-Z`、`a-z`、`0-9`、`_` のみ。空文字は不可。
- 基本の命名は `nameField`、`saveButton` のような **camelCase**。
- 大文字小文字は区別する。camelCase は命名規約で、許可文字の検査とは別。
- 同じ親の直下では Id を重複させない。別の親の下なら同じ Id を使用できる。

Id の重複や禁止文字は登録時にエラーにする。自動で連番を追加して別名に変えることはしない。

## パスで外側から辿る

```text
/demo
    /nameField
    /memoField
    /editDialog
        /nameField
        /saveButton
```

デモには同じ `nameField` があるが、次の完全パスなら一意になる。

```json
[
    "/demo/nameField",
    "/demo/editDialog/nameField"
]
```

先頭の `/` からルート Id、親 Id、対象 Id の順に辿る。区切りの `/` は Id 自体には含まれない。
同じページを複数配置するときも、その外側のコンテナーやページインスタンスに別々の Id を付ければよい。
短い Id の全体検索、ワイルドカード、`..`、末尾の `/` は現時点で対応しない。

将来はこのパスをスタイル設定の対象指定に使う予定。**今回の実装は Id の登録・解決・確認まで**で、
スタイル JSON から個別の文房具へルールを適用する機能はまだ実装していない。

## C# で組み込む場合

`StationeryNode` を親から作り、`DesktopUi` の `root` に渡す。
`AddTextBox` / `AddButton` の `parent` を指定すれば、同じ DesktopUi 内にも入れ子のコンテナーを作れる。
この階層は識別のためのもので、コンテナーを追加してもレイアウトや座標は自動変更しない。

```csharp
var root = new StationeryNode("demo");
var page = root.AddChild("mainPage", "page");
var ui = new DesktopUi(graphics, input, rasterizerFactory, page);
var name = ui.AddTextBox("nameField", new(0, 0, 300, 64), "名前");
var found = root.Resolve("/demo/mainPage/nameField");
ui.Focus.Focus(name.Path);
```

`Element.Id` はローカル名、`Element.Path` は完全パス。`DesktopUi.Focus` の登録・選択・キャプチャも完全パスで管理する。
以前の `ui.Focus.Focus("name")` のような呼び出しは `ui.Focus.Focus(element.Path)` に移行する。
`DesktopUi.Inspect` はゲームスレッドで呼び、取得したスナップショットを `StationeryDeveloperWindow.Show` / `Update` に渡す。
複数の DesktopUi をまとめるときは、共有の外側のノードを含めて一つのツリーにする。
開発者ウィンドウは Windows 用の独立した STA スレッドで動作し、ゲームの UI オブジェクトを直接操作しない。

現時点で自動登録の対象は `DesktopUi` で生成したコントロール。
描画ヘルパーや個別の MonoGame コントロールは、利用アプリ側で対応する `StationeryNode` とスナップショットを登録する。

デモの F12 は、読み込んだ `model` の階層を表示する。コードだけで作る上の例とは異なり、デモの原本には `mainPage` コンテナーを置かず、名前欄は `/demo/nameField` になる。model と layout の役割は [スタイル設定ガイド](stationery-style-settings.md) を参照。
