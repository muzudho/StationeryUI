# StationeryUI — 文房具UI

文房具をモチーフにした GUI ライブラリーと、  
その画面配置を設計する「スタイル設定エディター」のプロジェクトです。  
MonoGame を使い、設定・編集・制作などのツール向けのクロスプラットフォーム GUI を目指しています。

![デモ](./docs/user/images/screenshots/20260923_001531_v0_3_0_Demo.png)  
（👆 画面は開発中のものです。 v0.3.0 デモ）  

![開発者ウィンドウ](./docs/user/images/screenshots/20260923_001617_v0_3_0_DevWindow.png)  
（👆 画面は開発中のものです。 v0.3.0 開発者ウィンドウ）  

![スタイルエディター](./docs/user/images/screenshots/20260923_001803_v0_3_0_StyleDesigner.png)  
（👆 画面は開発中のものです。 v0.3.0 スタイルデザイナー）  


## 利用したい

配布物を使う方は、こちらからお読みください。

- **[すぐ使いたい — 最新のリリース](https://github.com/muzudho/StationeryUI/releases/latest)**：Assets からスタイル設定エディターの ZIP を入手。
- **[利用者向けドキュメント](docs/user/README.md)**：製品の説明、使われる場面、起動と操作。
- [これはいつどのように使われるか](docs/user/style-settings/use-case.md)：人間と AI が画面の調整を分担するシナリオ。
- [これはどのような製品か](docs/user/products.md)：エディターと文房具 UI の役割・対応範囲。

## NuGet から組み込む方・AI エージェントへ

パッケージを導入したら、まず [AI エージェント向け導入手順](docs/dev/nuget-agent-guide.md)を読んでください。F12 開発者ウィンドウの［指でつまむ］機能は、利用アプリ側でクリック判定・選択通知・桃色の枠の描画を接続する必要があります。

別ウィンドウを操作中に背後のゲームが反応しないよう、`Game.IsActive` をすべての UI とゲーム側のマウス入力処理に反映してください。非アクティブ時の入力抑止と復帰時の扱いも、同梱の導入手順に記載しています。

この手順と組み込みコードは NuGet パッケージ内の `docs/dev/` にも同梱します。インストール先の `<パッケージ ID>/<バージョン>/README.md` から参照できます。インストールしただけで AI が自動的に読む仕組みではないため、導入を担当するエージェントにはこの README と `docs/dev/nuget-agent-guide.md` を読むよう指示してください。

## 開発に貢献したい

ソースを改善する方、アプリへ文房具 UI を組み込む方は、こちらからお読みください。

- **[開発者向けドキュメント](docs/dev/README.md)**：組み込み、設計・実装、ビルドと検証への案内。
- [貢献方法](docs/dev/CONTRIBUTING.md)：変更・不具合報告の進め方。
- [開発日誌 — 2026年9月](docs/dev/log/2026/09.md)：最新の開発経緯。
- [配布・リリース手順](docs/dev/distribution/README.md)：成果物の作成と公開。

## ライセンス

[MIT License](LICENSE)。抽出元と依存物の表示は [第三者ライセンス](THIRD-PARTY-NOTICES.md)を参照してください。
