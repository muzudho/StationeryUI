# 文房具UIレイアウト構想

## 定義

* `ビューポート` - アプリケーションが表示されている矩形領域。（ウィンドウの枠などを除いたもの）
* `コントロール` - ［文房具UI］の［ボタン］や［テキストボックス］のこと。
* `レイアウトツリー` - ［文房具UI］の［コントロール］を配置するための位置情報を格納した、ツリー構造のデータ。


## 現行版と、新しい構想との違い

* ［スタイル設定ファイル］の models セクションに、コントロールの親子関係を設定しているが、これは廃止予定です。
* ［スタイル設定ファイル］の bindings セクションに、レイアウトの親子関係を定義しているが、これは廃止予定です。  
［コントロールハンドル］を用いて、コントロールとレイアウトの間接的なマッピングだけを行います。

［コントロール］は、それぞれ一意な［コントロールハンドル］を１つ持ちます。  
新しい構想の bindings セクションでは、［コントロールハンドル］に対して［レイアウトパス］を指定します。  
１つの［コントロール］が複数のページに配置される場合は、［レイアウトパス］の代わりに JSON オブジェクトを指定します。  
オブジェクトのキー名が［レイアウトキー］、値がそのキーに対応する［レイアウトパス］です。  


## MonoGame での座標系

［コントロール］は、以下のプロパティを持つ。  

* `Bounds` - MonoGame の `Rectangle` 型。実際に使われる座標。
	* `X`
	* `Y`
	* `Width`
	* `Height`
* `ControlHandle` - ［コントロール・ハンドル］。後述。
* `LayoutKey` - ［レイアウトキー］。後述。

画面に置く各コントロールへ、重複しない `ControlHandle` を割り当てます。

X, Y は、基本的にビューポート内での相対座標だ。  
左上が原点で、Y 軸は下向きに伸びる。  


## Bindings

［コントロール］の Bounds は、［ソルバー］によって設定される。  
［ソルバー］は、［コントロール・ハンドル］を bindings を使って［レイアウトパス］を取得し、  
［レイアウトツリー］の中の［レイアウトノード］を探し、  
そこから Bounds を計算する。


## 文房具UIレイアウト での座標系

１つのツリー構造になっている。  
ノードのことを［レイアウトノード］（ `LayoutNode` ）と呼ぶことにする。  
［要素］（ `Element` ）と呼ぶこともある。   

［レイアウトノード］は、抜粋すると以下の４つを持つ。  

* `Name` - ノードの名前。
* `Padding` - 要素の内側の余白。
* `Margin` - コンテントサイズの内側で、内側の要素の長さと寄せ方から決まる、内側の要素の両側の余白。  
   四辺の余白を直接指定することもできる。詳しくは後述。
* `Cells` - 子ノードのコレクション

```plaintext
┌── Your Element Bounds  ────────┐
│Padding 　　　　　　　　　　　　　　　　　│
│　┌─ ContentSize  ─────────┐　│
│　│Margin  　　　　　　　　　　　　　│　│
│　│　┌─ Child Element Bounds ─┐　│　│
│　│　│　　　　　　　　　　　　　│　│　│
│　│　└─────────────┘　│　│
│　│　　　　　　　　　　　　　　　　　│　│
│　└─────────────────┘　│
│　　　　　　　　　　　　　　　　　　　　　│
└─────────────────────┘
```


### Margin プロパティ

親要素の `Padding` を除いた領域をコンテントサイズとする。  
子要素の `Margin` は、このコンテントサイズの中で子要素の長さと寄せ方から決まる、子要素の両側の余白である。

`controlLength` と `align` を指定した場合、コンテントサイズから `controlLength` を引いた残りが両側の Margin になる。  
`align` に応じて、その余白を左右または上下に配分する。

まず、 JSON 設定方法を示します。

例：  ４方向ピクセル指定  

```plaintext
"margin" : {
	"top" : "10px",
	"right" : "20px",
	"bottom" : "30px",
	"left" : "40px"
}
```

例：　２軸ピクセル指定  

```plaintext
"margin" : {
	"horizontal": {
		"right" : "10px",
		"left" : "10px"
	},
	"vertical": {
		"top" : "10px",
		"bottom" : "10px"
	},
}
```

例：　１軸ピクセル指定、１軸寄せ指定  
```plaintext
"margin" : {
	"horizontal": {
		"right" : "10px",
		"left" : "10px"
	},
	"vertical": {
		"controlLength" : "20px",
		"align" : "center"
	},
}
```

例：　２軸寄せ指定  
```plaintext
"margin" : {
	"horizontal": {
		"controlLength" : "80px",
		"align" : "right"
	},
	"vertical": {
		"controlLength" : "20px",
		"align" : "center"
	},
}
```

計算例：  

左上を `(0, 0)` とし、コンテンツサイズが 300px x 200px で、子が以下のように指定されている場合、  

```plaintext
"margin" : {
	"horizontal": {
		"controlLength" : "80px",
		"align" : "right"
	},
	"vertical": {
		"controlLength" : "20px",
		"align" : "center"
	},
}
```

水平方向の余白は、300px - 80px = 220px。  
右寄せなので、左側の余白は 220px、右側の余白は 0px。  

垂直方向の余白は、200px - 20px = 180px。  
中央寄せなので、上側の余白は 90px、下側の余白は 90px。

子要素の Bounds は、以下のように計算される。  
X = 220px （左側の余白）  
Y = 90px （上側の余白）  
Width = 80px （水平の controlLength）  
Height = 20px （垂直の controlLength）  

解説：  

指定方法は、2種類ある。  

* `Directions` - ４方向指定系
	* メンバーは以下の４つ。	
		* `Left`
		* `Top`
		* `Right`
		* `Bottom`
	* 単位は
		* `px` - ピクセル
* `Axes` - ２軸指定系
	* メンバーは以下の２つ。
		* `Horizontal` - 水平方向
		* `Vertical` - 垂直方向
	* 上のメンバーは、更に以下の２種類のメンバーを持ちます。
		* marginType 型
			* 水平方向なら `Left` と `Right`。
			* 垂直方向なら `Top` と `Bottom`。
		* alignType 型
			* `controlLength` - コントロールの横幅（水平方向の場合）または縦幅（垂直方向の場合）
				* 制約 - `controlLength` が コンテントサイズを超えた場合、 `Error` ラベル表示とします。
			* `align` - 寄せる位置です。以下の５つのいずれかを指定します。
				* `top` - 上寄せ。余白のすべてを下側に配分します。
				* `right` - 右寄せ。余白のすべてを左側に配分します。
				* `bottom` - 下寄せ。余白のすべてを上側に配分します。
				* `left` - 左寄せ。余白のすべてを右側に配分します。
				* `center` - 中央寄せ。余白を左右または上下に均等に配分します。
				* 制約 - `horizontal`  では、`left`, `right`, `center` だけ指定できます。
				* 制約 - `vertical`  では、`top`, `bottom`, `center` だけ指定できます。
* 制約 - `Left`, `Right` と、`Horizontal` は同時に指定できません。
* 制約 - `Top`, `Bottom` と、`Vertical` は同時に指定できません。

### Padding プロパティ

まず、 JSON 設定方法を示します。  

```json
{
	"top" : "10px",
	"right" : "20px",
	"bottom" : "30px",
	"left" : "40px"
}
```

解説：  

* メンバーは以下の４つ。	
	* `Left`
	* `Top`
	* `Right`
	* `Bottom`
* 単位は
	* `px` - ピクセル
* 省略すると 0px になります。

### Cells プロパティ

いわゆる `ChildNodes` に相当します。  
以下の３つの種類があります。  

* ボックス・レイアウト（BoxLayout） - 子要素を０～１つ持つ。
	* 詳しくは： 📖 [layouts セクションのボックス・レイアウト](../dev/structure/style-settings-file/layouts/box-layout.md)
* グリッド・レイアウト（GridLayout） - 行と列を持つ。子要素を０～複数持つ。
	* 詳しくは： 📖 [layouts セクションのグリッド・レイアウト](../dev/structure/style-settings-file/layouts/grid-layout.md)
* ドック・レイアウト（DockLayout） - 四辺と中央に子要素を積む。四辺には０～複数個積める。中央は子要素を０～１個持つ。
	* 詳しくは： 📖 [layouts セクションのドック・レイアウト](../dev/structure/style-settings-file/layouts/dock-layout.md)
	* 詳しくは： 📖 [layouts セクションのドック・レイアウト（詳細）](../dev/structure/style-settings-file/layouts/dock-layout-detail.md)
* タブド・ボックス・レイアウト（TabbedBoxLayout） - 子要素を同時に１つしか持てません。遷移することで、子要素を配列で複数個持たせることができます。
	* 詳しくは： 📖 [layouts セクションのタブド・ボックス・レイアウト](../dev/structure/style-settings-file/layouts/tabbed-box-layout.md)
* その他
	* 詳しくは： 📖 [ネステッド・レイアウト](../dev/structure/style-settings-file/layouts/nested-layouts.md)
		* グリッドのセルを跨ぐ考え方

## レイアウトツリー

［レイアウトノード］に［レイアウト］を指定し、子要素の［レイアウトノード］をぶら下げると、  
ツリー構造になります。  

例：  

```plaintext
* root: viewPort - TabbedBoxLayout [ControlHandle: viewPort]
	* 0: topDemoPage - DockLayout [ControlHandle: topDemoPage]
		* top: applicationBar - GridLayout [ControlHandle: applicationBar]
		* center: mainContent - GridLayout [ControlHandle: mainContent]
			* 1y.1x.1w.1h: leftMenu - BoxLayout [ControlHandle: leftMenu]
				* single: nameField [ControlHandle: nameField]
			* 1y.2x.2w.1h: rightContent - BoxLayout [ControlHandle: rightContent]
		* bottom: inspectorPanel - BoxLayout [ControlHandle: inspectorPanel]
	* 1: splitPaneDemoPage - DockLayout [ControlHandle: splitPaneDemoPage]
		* top: applicationBar - GridLayout [ControlHandle: applicationBar]
	* 2: layoutDemoPage - DockLayout [ControlHandle: layoutDemoPage]
		* top: applicationBar - GridLayout [ControlHandle: applicationBar]
```

👆　上記の１行が［レイアウトノード］です。  

解説：  

```plaintext
* root: viewPort - TabbedBoxLayout
```

👆　`root:` は、最上位のノードを示します。  
`viewPort` は、ノードの名前です。  
`TabbedBoxLayout` は、レイアウトの種類です。  

ボックスレイアウトは、 `single:` が唯一の子ノードを示します。  

`top:` は、ドックレイアウトが持つ上側の子ノードを示します。  
他に、`right:`, `bottom:`, `left:`, `center:` も同様です。  

`1y.1x.1w.1h:` は、グリッドレイアウトが持つ子ノードの位置とサイズを示します。  
`1y` は、上から１行目に配置することを意味します。  
`1x` は、左から１列目に配置することを意味します。  
`1w` は、横幅が１列分であることを意味します。  
`1h` は、縦幅が１行分であることを意味します。


## バインディングズ

まず、 JSON 設定方法を示します。  

```json
{
    "bindings": {
		"ctrlViewPort": "root",
		"ctrlTopDemoPage": "root:viewPort/0",
		"ctrlApplicationBar": "root:viewPort/0:topDemoPage/top",
		"ctrlMainContent": "root:viewPort/0:topDemoPage/center",
		"ctrlLeftMenu": "root:viewPort/0:topDemoPage/center:mainContent/1y.1x.1w.1h",
		"ctrlNameField": "root:viewPort/0:topDemoPage/center:mainContent/1y.1x.1w.1h:leftMenu/single",
		"ctrlRightContent": "root:viewPort/0:topDemoPage/center:mainContent/1y.2x.2w.1h",
		"ctrlInspectorPanel": "root:viewPort/0:topDemoPage/bottom",
		"ctrlSplitPaneDemoPage": "root:viewPort/1",
		"ctrlLayoutDemoPage": "root:viewPort/2"
	}
}
```

👆　書式は、  

```plaintext
"コントロールハンドル": "レイアウトノードのパス"
```

です。  

［コントロールハンドル］は、画面上で一意な任意の文字列です。  
使える文字は、半角英数字記号です。空白、改行は使えません。  
［レイアウトノードの名前］と区別しやすくするために、 `ctrl` で始めることを推奨します。  

［コントロール］は、１つの［レイアウトキー］につき［レイアウトパス］を１つ持ちます。  

［レイアウトキー］は、コントロールで一意な任意の文字列です。  
使える文字は、半角英数字記号です。空白、改行は使えません。  
他の名前と区別しやすくするために、 `lyt` で始めることを推奨します。  


［コントロール］によっては、別ページにも配置されることがあります。  
この場合、bindings の値に［JSON オブジェクト］を置き、複数の［レイアウトキー］と［レイアウトパス］のペアを  
持たせます。  

例：  

```json
{
    "bindings": {
		"ctrlViewPort": "root",
		"ctrlSaveButton": {
			"lytTopDemoPage" : "root:viewPort/0:topDemoPage/top:applicationBar/1y.1x.1w.1h",
			"lytSplitPaneDemoPage" : "root:viewPort/1:splitPaneDemoPage/top:applicationBar/1y.1x.1w.1h",
			"lytLayoutDemoPage" : "root:viewPort/2:layoutDemoPage/top:applicationBar/1y.1x.1w.1h"
		}
	}
}
```

👆　３つのページに配置される［コントロール］の指定例。  
C# ソースコードでは［コントロール］の `LayoutKey` にキー名を設定します。  
ソルバーはその［レイアウトキー］に対応する［レイアウトパス］を使って Bounds を計算します。


## ソルバー

［レイアウトノードのパス］を辿ると、X, Y, Width, Height が決まります。  
これを［コントロール］に上書き（反映）させるのが、ソルバーの仕事です。  

例えば、ページを開いたとき、またはウィンドウのサイズが変わったときに、ソルバーを呼び出すことで、  
コントロールの Bounds を更新します。  

計算順序：  

```plaintext
┌── ViewPort = RootNode Bounds Width 800px ────────┐
│Padding All 10 pixels   　　　　　　　　　　　　　　　　　　│
│　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　Height 600px
│　┌─ RootNode's ContentSize Width 780px  ─────┐　　 │
│　│Margin All 20 pixels　　　　　　　　　　　　　　 │　　 │
│　│　　　　　　　　　　　　　　　　　　　　　　　　 Height 580px
│　│　┌─ Child Node Bounds Width 740px ──┐　　　│　　 │
│　│　│　　　　　　　　　　　　　　　　　　　│　　 │　　 │
│　│　│　　　　　　　　　　　　　　　　　　 Height 540px 　│
│　│　│　　　　　　　　　　　　　　　　　　　│　　 │　　 │
│　│　└───────────────────┘　　 │　　 │
│　└────────────────────────┘　　　│
└──────────────────────────────┘
```

ツリーの根の［レイアウトノード］から順に、子の［レイアウトノード］を計算していきます。  

ビューポートのサイズが最大のサイズです。  
例えば、ビューポートのサイズが 800px x 600px の場合、ルートノードの Bounds は 800px x 600px です。  
ルートノードに Margin は有りません。  

ルートノードの Bounds から、Padding を引いた残りが、ルートノードのコンテントサイズになります。  
例えば、ルートノードの四方の Padding が 10px の場合、ルートノードのコンテントサイズは 780px x 580px です。  

ルートノードのコンテントサイズから、子ノードの Margin を引いた残りが、子ノードの Bounds になります。  
例えば、子ノードの四方の Margin が 20px の場合、子ノードの Bounds は 740px x 540px です。  

あとは繰り返しです。  


## 廃止方針

現在の［スタイル設定ファイル］が持つ、  
D:\github.com\muzudho\StationeryUI\App_Data\demo.stationery-style.json  

`models` セクションは、将来廃止予定です。  
代わりに、［コントロール・ハンドル］を［コントロール］に設定してください。  
