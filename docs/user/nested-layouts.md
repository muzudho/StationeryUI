# レイアウトをネストする

`layouts` の定義に `children` を置くと、レイアウトの内側に別のレイアウトを配置できます。リポジトリー版の拡張機能です。旧配布版のエディターでは読み込めないため、対応版を使用してください。

| 親の種類 | 子の数 | 子が使う領域 |
| --- | --- | --- |
| `box-layout` | 0～1個 | 親の padding を除いた内側全体 |
| `grid-layout` | 0～複数個 | 子が指定した行・列の範囲 |

子には `box-layout` または `grid-layout` を指定でき、その中にさらに子を置けます。`split-pane` とページレイアウトのネストは対象外です。

## セルの位置と大きさ

親がグリッドの場合、子自身に次を指定します。

| 項目 | 意味 | 省略時 |
| --- | --- | --- |
| `row` | 開始行。0始まり | 0 |
| `col` | 開始列。0始まり | 0 |
| `rowspan` | 占有する行数 | 1 |
| `colspan` | 占有する列数 | 1 |

行・列は0以上、行数・列数は1以上の整数です。範囲外や、兄弟・同じグリッドに直接配置された文房具との重なりはエラーになります。空いているセルへ自動移動したり、自動的に表を広げたりはしません。

これらは**親のグリッド**を参照します。子の `row-definitions`・`column-definitions` は、割り当てられた領域の内側をどう分けるかを指定します。親で2行×2列を占有し、内部は1行×1列にできます。親がボックスの場合は行・列がないので、この4項目を指定しません。

## 設定例

```json
{
    "models": [
        {
            "id": "demo",
            "type": "viewport",
            "children": [
                {
                    "id": "message",
                    "type": "textBlock"
                }
            ]
        }
    ],
    "layouts": [
        {
            "id": "demoViewport",
            "type": "box-layout",
            "padding": {
                "top": "8px",
                "right": "8px",
                "bottom": "8px",
                "left": "8px"
            },
            "children": [
                {
                    "id": "mainGrid",
                    "type": "grid-layout",
                    "row-definitions": [
                        "1rate",
                        "2rate"
                    ],
                    "column-definitions": [
                        "100px",
                        "1rate"
                    ],
                    "children": [
                        {
                            "id": "inspectorContents",
                            "type": "grid-layout",
                            "row": 0,
                            "col": 0,
                            "rowspan": 2,
                            "colspan": 2,
                            "row-definitions": [
                                "1rate"
                            ],
                            "column-definitions": [
                                "1rate"
                            ],
                            "slots": [
                                {
                                    "id": "message",
                                    "row": 0,
                                    "col": 0
                                }
                            ]
                        }
                    ]
                }
            ]
        }
    ],
    "bindings": [
        {
            "layout": "/demoViewport/mainGrid/inspectorContents",
            "parentModel": "demo",
            "childrenModel": [
                {
                    "model": "message",
                    "slot": "message"
                }
            ]
        }
    ]
}
```

これはネストの形式を示す汎用の設定例です。デモアプリ固有の必須部品を含む設定の代わりには使いません。

`demo` の領域を基準に `demoViewport` を配置し、四辺の8pxを除いた領域へ `mainGrid` を配置します。`inspectorContents` はその2行×2列を使い、内部の1セルへ `message` を配置します。親の余白は一度だけ差し引きます。セルの大きさが異なる場合も、跨る行高・列幅の実寸を合計します。

## Id と参照

レイアウト Id は**同じ親の下で一意**にします。別の親なら同じ Id を使えます。Id 自体には英字・数字・アンダースコアを使用し、ドットは含めません。

`bindings.layout` は最上位からの先頭 `/` 付きのスラッシュ区切りの完全パスです。モデルと同じ区切り方ですが、レイアウト定義のツリーを参照します。上の例では `/demoViewport/mainGrid/inspectorContents` を指定し、`inspectorContents` だけでは参照できません。

同じモデルを基準に、同じレイアウトツリーの複数のグリッドへ binding を書けます。その場合は各 binding の `parentModel` を揃えます。親レイアウトにも binding を重複して書く必要はありません。別のモデルを基準に同じツリーを使うと、独立した配置になります。

文房具の配置枠は `layouts.slots` に定義します。枠は `id`、開始位置の `row` と `col` が必須で、`rowspan`・`colspan` の省略時は1です。`bindings.childrenModel` は `slot` と `model` の対応だけを書きます。従来の `column` も `col` の別名として使えますが、両方を同時に指定するとエラーです。

## エディターで操作する

- ツリーの `layouts`、子のないボックス、またはグリッドを選び、［子要素追加］を押します。
- Id と種類を指定します。グリッドの子を追加する場合は、`row`・`col`・`rowspan`・`colspan` も入力します。
- 子レイアウトを選ぶと、その行高・列幅や余白を編集できます。プレビューは親から割り当てられた実際の範囲を使います。
- グリッド直下の子の位置・範囲を変更するときは［Ｉｄ変更］を開きます。同じIdのままセル指定だけ変更できます。
- 親のIdを変更すると、子孫を指す binding のパスも更新します。参照中の子を含むレイアウトは、そのまま削除できません。

エディターは `models`・`bindings` を自動生成して既存部品を子へ移し替えることはしません。既存の文房具で埋まったセルに子を追加する場合は、対応付けも調整する必要があります。

[スタイル設定の仕様](stationery-style-settings.md) ／ [エディターの使い方](style-designer.md) ／ [利用者向け目次](README.md)
