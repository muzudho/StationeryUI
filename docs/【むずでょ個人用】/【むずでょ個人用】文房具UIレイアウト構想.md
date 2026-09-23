# 文房具UIレイアウト構想

## 定義

* `ビューポート` - アプリケーションが表示されている矩形領域。（ウィンドウの枠などを除いたもの）
* `コントロール` - ［文房具UI］の［ボタン］や［テキストボックス］のこと。

## MonoGame での座標系

コントロールは、以下のプロパティを持つ。  

* `Bounds` - MonoGame の `Rectangle` 型。
	* `X`
	* `Y`
	* `Width`
	* `Height`

X, Y は、基本的にビューポート内での相対座標だ。  
左上が原点で、Y 軸は下向きに伸びる。  

## 文房具UIレイアウト での座標系

１つのツリー構造になっている。  
ノードのことを［レイアウトノード］（ `LayoutNode` ）と呼ぶことにする。  

［レイアウトノード］は、抜粋すると以下の３つを持つ。  

* `Margin` - 外側の余白。
* `Padding` - 内側の余白。
* `CellCollection` - 子ノードのコレクション

### Margin

まず、 JSON 設定方法を示します。

例：  ４方向ピクセル指定  

```plaintext
"margin" : {
	"top:" : "10px",
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
		"top:" : "10px",
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
		* `rate` - 比
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
			* `align` - 寄せる位置です。以下の５つのいずれかを指定します。
				* `top` - 上寄せ
				* `right` - 右寄せ
				* `bottom` - 下寄せ
				* `left` - 左寄せ
				* `center` - 中央寄せ


