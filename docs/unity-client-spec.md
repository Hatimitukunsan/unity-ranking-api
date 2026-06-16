# Unityクライアント仕様

## 概要

UnityからランキングAPIを利用するためのクライアント画面です。
ユーザー名とスコアを送信し、ランキング一覧と自分の順位を確認できます。

この画面は、UnityとWeb APIの連携を確認するためのシンプルなクライアントです。
ゲーム本体の演出や複雑なUIよりも、API通信の流れが分かることを重視しています。

## 画面構成

画面は2カラム構成です。

- 左側: スコア送信フォーム
- 右側: ランキング表示
- 下部: API接続状態

### スコア送信フォーム

入力項目:

- ユーザー名
- スコア

操作:

- `Submit` ボタンでスコアを送信します。

表示:

- 送信可能状態
- 送信中
- 送信成功
- 自分の順位
- エラー内容

### ランキング表示

表示内容:

- 上位10件のランキング
- 順位
- ユーザー名
- スコア

スコア送信後、自分のスコアが上位10件に含まれる場合は、その行を強調表示します。
上位10件に含まれない場合でも、上位10件の下に自分の順位を追加表示します。

例:

```txt
1.  ore          12345
2.  Nova          9870
...
10. Yui           9050
...
58. test6         1256
```

### API接続状態

画面下部にAPIの状態を表示します。

例:

```txt
API Status: 接続OK | API Base URL: http://localhost:8080
```

## 利用するAPI

### スコア送信

```txt
POST /scores
```

Request:

```json
{
  "username": "unity_player",
  "score": 1200
}
```

Response:

```json
{
  "username": "unity_player",
  "score": 1200,
  "id": 1
}
```

### ランキング取得

```txt
GET /ranking?limit=10
```

Response:

```json
[
  {
    "username": "ore",
    "score": 12345,
    "id": 1
  }
]
```

### 自分の順位取得

```txt
GET /scores/{score_id}/rank
```

Response:

```json
{
  "username": "unity_player",
  "score": 1200,
  "id": 1,
  "rank": 58
}
```

## 動作確認の流れ

1. Docker ComposeでAPIサーバーとDBを起動します。

```powershell
docker compose up
```

2. 必要に応じてサンプルデータを投入します。

```powershell
docker compose exec api python seed.py seed_data/manual_test_scores.json --clear
```

3. Unityで `RankingClientScene` を開きます。

4. Playして、ユーザー名とスコアを入力します。

5. `Submit` を押して、送信結果、自分の順位、ランキング表示を確認します。

## 注意点

Unity Editorから実行する場合、APIの接続先は以下です。

```txt
http://localhost:8080
```

スマートフォン実機やWebGLで動かす場合、`localhost` の意味が変わるため、接続先URLの見直しが必要です。
