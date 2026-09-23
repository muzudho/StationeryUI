# Ｉｄとパス

- Id は空でない `[A-Za-z0-9_]+`。大小文字を区別します。camelCase を推奨します。
- 同じ親の直下のモデル Id は一意にします。異なる親の下なら同じ Id を使えます。
- レイアウト Id は同じ親の下で一意にします。モデル Id とは別の名前空間です。`bindings.layout` は `/frame/grid/inner` のような先頭 `/` 付きのスラッシュ区切りの完全パスで参照します。
- `model` / `parentModel` はルートからのパスです。`app/pageA` と `/app/pageA` は同じ参照です。
- `childrenModel[].model` は `parentModel` からの相対パス、または `/` で始まる完全パスです。
- `firstModel` / `secondModel` / `inspectorModel` も、その binding の `model` からの相対パスか完全パスです。
- `..`、ワイルドカード、全ツリーからの短い Id 検索はありません。

パーサーは数字始まりや非 camelCase も許可します。スタイル設定エディターはそれらに警告を表示しますが、警告表示はパーサーの機能ではありません。`StationeryNode.Resolve` を直接呼ぶ場合は、JSON の相対参照と違い **完全パス**を渡します。
