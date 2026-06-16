# Unityクライアント実装説明

## 実装方針

Unityクライアントは、API通信、画面制御、表示部品を分けて実装しています。

目的は、処理の流れを読みやすくし、あとからUIや通信処理を変更しやすくすることです。

## ディレクトリ構成

```txt
unity-client/ranking-client/Assets/
  Scenes/
    RankingClientScene.unity
  Scripts/
    Api/
      RankingApiClient.cs
      RankingApiException.cs
      RankingModels.cs
    UI/
      RankingClientController.cs
      ScoreSubmitView.cs
      RankingListView.cs
      ApiStatusView.cs
```

## namespace

Unity側のコードでは、以下のnamespaceを使っています。

```csharp
Hakusa.RankingClient.Api
Hakusa.RankingClient.UI
```

`Hakusa.RankingClient.Api` はAPI通信関連、`Hakusa.RankingClient.UI` は画面表示と操作関連です。

## クラスごとの役割

### `RankingApiClient`

FastAPIサーバーとHTTP通信するクラスです。

担当:

- `POST /scores`
- `GET /ranking`
- `GET /scores/{score_id}/rank`
- JSONの送受信
- HTTPエラーの検出

UIを直接操作しないようにしています。
これにより、通信処理と画面表示の責務を分けています。

### `RankingApiException`

API通信で失敗したときに使う独自例外です。

HTTPステータスコードやレスポンス本文を保持することで、UI側やConsoleログで原因を確認しやすくしています。

### `RankingModels`

APIのRequest/Responseに対応するデータクラスを定義しています。

主なモデル:

- `ScoreCreateRequest`
- `ScoreResponse`
- `ScoreRankResponse`

Unityの `JsonUtility` で扱いやすいように、プロパティではなくpublicフィールドを使っています。

### `RankingClientController`

画面全体の流れを制御するクラスです。

担当:

- Submitボタンが押されたときの処理
- Refreshボタンが押されたときの処理
- API通信の呼び出し
- 通信結果を各Viewに渡す
- 最後に投稿した自分のスコアを保持する

処理の流れ:

```txt
Submit
  -> POST /scores
  -> GET /scores/{id}/rank
  -> GET /ranking?limit=10
  -> UI更新
```

### `ScoreSubmitView`

左側のスコア送信エリアを担当するViewです。

担当:

- ユーザー名の入力取得
- スコアの入力取得
- 入力値の簡単なチェック
- 送信中、成功、失敗の表示
- 通信中に入力欄とボタンを無効化する

API通信は行わず、ボタン操作をControllerへ通知します。

### `RankingListView`

右側のランキング表示を担当するViewです。

担当:

- 上位ランキングの表示
- 自分のスコア行の強調表示
- ランキング圏外の場合の自分の順位表示
- Refreshボタンの操作通知

ランキング表示は、順位、ユーザー名、スコアを別々のTextに分けています。
これにより、名前とスコアの縦位置が揃いやすくなります。

### `ApiStatusView`

画面下部のAPI接続状態を表示するViewです。

担当:

- 待機中
- 通信中
- 接続OK
- エラー
- API Base URLの表示

## 非同期処理

非同期処理にはUniTaskを使用しています。

理由:

- `async/await` で処理順が読みやすい
- `POST -> 順位取得 -> ランキング更新` の流れを表現しやすい
- Unityの非同期処理として扱いやすい

例:

```csharp
ScoreResponse postedScore = await apiClient.PostScoreAsync(...);
ScoreRankResponse rank = await apiClient.GetRankAsync(postedScore.id, ...);
ScoreResponse[] ranking = await apiClient.GetRankingAsync(...);
```

## JSON処理

JSONの変換にはUnity標準の `JsonUtility` を使っています。

ただし、`JsonUtility` はJSON配列を直接扱いにくいため、ランキング取得時は一度以下のような形に包んでから変換しています。

```json
{
  "items": [
    {
      "username": "ore",
      "score": 12345,
      "id": 1
    }
  ]
}
```

## エラー処理

通信に失敗した場合は、画面に短いエラーを表示し、詳細はUnity Consoleに出します。

画面:

```txt
ランキング取得に失敗しました
```

Console:

```txt
HTTP status
response body
exception details
```

ユーザーに見せる情報と、開発者が確認する情報を分けています。

## Git管理方針

Gitで管理するもの:

- `Assets/Scenes/`
- `Assets/Scripts/`
- `Packages/`
- `ProjectSettings/`

Gitで管理しないもの:

- `Library/`
- `Temp/`
- `Logs/`
- `UserSettings/`
- `.vs/`
- `.sln`
- `.csproj`

Unity Editor用のUI自動生成スクリプトは、最終成果物には含めていません。
完成したSceneと実行時コードだけを公開対象にしています。
