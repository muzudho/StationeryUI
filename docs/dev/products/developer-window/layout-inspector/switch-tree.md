# ツリー表示の切替

ツリー上部で以下の２つを切り替えることができる。  

* モデルツリー
* レイアウトツリー

## モデルツリー

`(Id : 種類)` のみ表示。  

## レイアウトツリー

配置情報付きのラベルと実際のネストしたレイアウト定義を表示。  

レイアウトツリーでは、文房具とその所有するルートレイアウトを１行にまとめる。  
例えば `(demoPage : Page) (- : gridLayout)` の下へ配置されたモデルを直接表示し、  
同じ gridLayout のノードは重ねて表示しない。  
ルートの内部にネストしたレイアウトは別ノードとして残す。

## その他

`DeveloperInspectionLayout.Apply(entries, settings, arranged)`（または `ui.Inspect(settings, arranged)`）でスナップショットへ `LayoutNodes` と `LayoutParentPath` を付与する。  
既存のモデル配列へレイアウトを直接追加しないため、画面のキャプチャー判定はモデルだけを対象にする。  

レイアウトの識別子は `ownerModelPath + ":" + layout.Path`。そのノードの `BoxModel` は定義自身の margin・padding を表す。  
付加情報のないスナップショットも表示できるが、定義の入れ子は表示できない。
`DeveloperViewState` には表示モードと両ツリーの選択・開閉状態を保存する。  
操作ログの `TreeMode` は Model=0、Layout=1。

レイアウトの桃色の枠には、画面の配置に使った最新の `StationeryLayoutResult` を `arranged` として検査データへ渡す。座標はウィンドウのピクセル単位で、ズーム変換を重ねて掛けない。
枠の対象は `DeveloperInspectionLayout.FindVisibleEntry(entries, selectedPath)` で検索し、その `WindowBounds` を `DrawInspectionOutline` に渡す。モデルだけの検索ではレイアウトノードが見つからない。キャプチャーのヒット判定には従来のモデル配列を使う。

統合ノードの識別子は文房具のパスを使い、外周・分割点線・margin／padding を確認できる。  
詳細欄の「所有レイアウト」で定義パスも確認できる。  
古いルートレイアウトの選択・開閉パスは所有モデルへ読み替える。  
検査データの `LayoutNodes` は維持し、表示上の階層だけをまとめる。
