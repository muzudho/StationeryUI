# デモの解説

## レイアウト

トップとレイアウトページは `pageDock`、スプリットページは `pageDockFullscreen` を使う。  
サイズ差は layouts 内に定義する。各ページ直下に `body` と `inspectorPanel` を置き、  
配列の先頭で bottom のパネルを確保し、center の本文に残りを渡す。  
トップとレイアウトデモは80px、スプリットペーンデモは0px。padding 付きの本文の grid は body に結び付ける。  
`ContentBounds[page.Path + "/body"]` が余白を除いた本文領域になる。

設定原本と C# 内の fallback は同じ構成。  
入力・ページ移動・分割操作は既存の部品 Id で接続し、完全パスには `/body/` が加わる。  
`PageLayoutTests` は本文・下端のサイズ、狭い画面、サイズ変更のリロード、検査ツリー、  
fallback の一致を検証する。設定方法は [ドック配置](dock-layout.md)を参照。
