# 文房具ＵＩのレイアウト設定

## 将来的に

例えば `StationeryUI\App_Data\demo.stationery-layout.json` といったファイルで設定できるようにしたい。  
JSON形式とする。  

## 画面レイアウトのルート要素は viewport

```json
{
  "viewport": {
  }
}
```

👆　とりあえず、 `viewport` オブジェクト要素を作る。  


例えば、パディングの設定は以下のようにする。  

```json
{
    "viewport": {
        "padding": {
            "top": "8px",
            "right": "8px",
            "bottom": "8px",
            "left": "8px"
        }
    }
}
```

内側の要素は `contents` 配列にまとめる。  

```json
{
    "viewport": {
        "contents": [
        ]
    }
}
```

👆　仕様はここまで。作りかけ。  
