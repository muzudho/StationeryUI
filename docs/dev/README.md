# 開発者向けドキュメント

StationeryUI自体の開発・保守を行う方向けの資料です。ライブラリーをアプリに組み込む場合は [利用者向けドキュメント](../user/README.md) を参照してください。

- [設計と境界](architecture.md)
- [コントロールのプログラム解説](control-guide.md)
- [アンダーラインのアクションバッジ](action-badges.md)
- [抽出元一覧](extraction-inventory.md)
- [実装・引き継ぎ計画](implementation-plan.md)
- [検証記録](validation.md)
- [貢献方法](CONTRIBUTING.md)
- [開発日誌：2026年9月](log/2026/09.md)

## ビルドと検証

リポジトリーのルートで実行します。

```powershell
dotnet build StationeryUI.slnx -c Release
dotnet run --project tests/StationeryUI.Tests -c Release
dotnet run --project tests/StationeryUI.Windows.Tests -c Release
dotnet pack StationeryUI.slnx -c Release -o artifacts/packages
```

実IMEやDPIの手動確認手順は [検証記録](validation.md) を参照してください。

## 開発日誌の配置

開発日誌は `docs/dev/log/YYYY/MM.md` に月単位で記録します。
