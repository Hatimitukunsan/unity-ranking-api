# Unity Ranking API

Unityクライアントからスコアを送信し、FastAPIサーバーでランキングを管理するプロジェクトです。
ランキングデータはPostgreSQLに保存し、開発環境はDocker Composeで再現できる構成にします。

このプロジェクトでは、UnityとWeb APIの連携だけでなく、サーバー側のAPI設計、DB設計、Dockerを使った開発環境構築までを扱います。

## 技術構成

| Area | Technology |
|---|---|
| Client | Unity |
| API Server | FastAPI |
| Language | Python |
| Database | PostgreSQL |
| Development Environment | Docker Compose |

## 実装予定の機能

- Unityからのユーザー名とスコアの送信
- スコア順のランキング取得
- PostgreSQLによるスコアの永続化
- 入力値のバリデーション
- 簡単な不正対策
- Docker Composeによるローカル開発環境の構築

## ディレクトリ構成

```txt
unity-ranking-api/
  README.md
  compose.yaml
  server/
    Dockerfile
    main.py
    requirements.txt
  unity-client/
```

## 開発状況

現在はFastAPIサーバーをDocker Composeで起動できる段階です。
今後、PostgreSQL接続、ランキングAPI、UnityクライアントのHTTP通信処理を順番に実装していきます。

## 起動方法

Docker Desktopを起動した状態で、リポジトリ直下から以下を実行します。

```powershell
docker compose up
```

起動後、ブラウザで以下にアクセスします。

```txt
http://localhost:8080
http://localhost:8080/docs
```

停止する場合は以下を実行します。

```powershell
docker compose down
```

## 現在のAPI

### `GET /`

動作確認用のエンドポイントです。

Response:

```json
{
  "message": "Hello World"
}
```
