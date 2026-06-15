# Unity Ranking API

Unityクライアントからユーザー名とスコアを送信し、FastAPIサーバーでランキングを管理するプロジェクトです。
ランキングデータはPostgreSQLに保存し、開発環境はDocker Composeで再現できる構成にします。

このプロジェクトでは、UnityとWeb APIの連携だけでなく、サーバー側のAPI設計、DB設計、Dockerを使った開発環境構築までを扱います。

## 技術構成

| Area | Technology |
|---|---|
| Client | Unity |
| API Server | FastAPI |
| Language | Python |
| Database | PostgreSQL |
| Database Tool | Adminer |
| Development Environment | Docker Compose |
| Editor Environment | Dev Containers |

## 実装予定の機能

- Unityからのユーザー名とスコアの送信
- スコア順のランキング取得
- PostgreSQLによるスコアの永続化
- 入力値のバリデーション
- 簡単な不正対策
- Docker Composeによるローカル開発環境の構築

## ディレクトリ構成

- `README.md`
- `compose.yaml`
- `.env.example`
- `.devcontainer/`
  - `devcontainer.json`
  - `docker-compose.yml`
- `server/`
  - `Dockerfile`
  - `main.py`
  - `requirements.txt`
- `unity-client/`

## 現在の開発状況

現在は、Docker Composeで以下のサービスをまとめて起動できる段階です。

- `api`: FastAPIサーバー
- `db`: PostgreSQL
- `adminer`: ブラウザからPostgreSQLを確認するための開発用ツール

FastAPIのAPI実装は、現時点では動作確認用の `GET /` のみです。
今後、FastAPIからPostgreSQLへ接続し、ランキング登録・取得APIを実装していきます。

## 環境変数

このプロジェクトでは、ローカル環境ごとに変わる値を `.env` に書きます。
`.env` はGit管理せず、見本として `.env.example` を管理します。

初回は、`.env.example` をコピーして `.env` を作成する必要があります。

## Dev Containers

VS Code Dev Containersを使うことで、Docker Composeの `api` サービスを開発環境として利用できます。

Dev Containerでは、VS Codeはコンテナ内の `/workspace` を開きます。
また、ローカルの `server/` はコンテナ内の `/app` にマウントされます。

```txt
/workspace
  リポジトリ全体を確認するための場所

/app
  FastAPIサーバーの実行・編集に使う場所
```

利用する場合は、Docker Desktopを起動した状態でVS Codeから以下を実行します。

```txt
Dev Containers: Rebuild and Reopen in Container
```

## 起動方法

Docker Desktopを起動した状態で、リポジトリ直下から以下を実行します。

```
docker compose up
```

Dockerfileや依存パッケージを変更した後は、必要に応じて以下を実行します。

```
docker compose up
```

起動後、ブラウザで以下にアクセスすることができます。


- FastAPI: `http://localhost:8080`
- FastAPI Swagger UI: `http://localhost:8080/docs`
- Adminer: `http://localhost:8081`

## Adminerの接続情報

Adminerでは、`.env` をデフォルト値にしている場合、以下の内容でPostgreSQLに接続することができます。

```txt
System: PostgreSQL
Server: db
Username: postgres
Password: example_password
Database: ranking
```

## PostgreSQLの確認

PostgreSQLの対話モードに入るために、以下のように`psql` を実行することができます。

```powershell
docker compose exec db psql -U postgres -d ranking
```

## 停止方法
コンテナを停止する場合は以下を実行します。

```
docker compose down
```

PostgreSQLのデータは名前付きvolume `postgres-data` に保存されるため、通常の `docker compose down` では削除されません。

データも含めて削除したい場合のみ、以下を実行します。

```
docker compose down -v
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

## 次に行う作業
1. FastAPIからPostgreSQLへ接続

   PythonのDB接続ライブラリを追加し、FastAPIからPostgreSQLへ接続できるようにします。
   この段階で、DB接続情報をFastAPI側でも環境変数から読み込むようにします。

2. ランキング用テーブルの作成

   ユーザー名、スコア、登録日時を保存するテーブルを作成します。
   最初は手動SQLで理解し、その後マイグレーションツールを使うか検討します。

3. ランキングAPIの実装

   以下のAPIを実装します。

   ```txt
   POST /scores
   GET /ranking
   ```

   `POST /scores` ではユーザー名とスコアを登録します。
   `GET /ranking` ではスコアの高い順にランキングを返します。

4. UnityからHTTP通信

   Unityの `UnityWebRequest` を使い、スコア送信とランキング取得を行います。

## 参考ドキュメント

- [Docker Compose](https://docs.docker.com/compose)
- [Dev Containers](https://code.visualstudio.com/docs/devcontainers/containers)
- [Postgres Docker Official Image](https://hub.docker.com/_/postgres)
- [Adminer Docker Official Image](https://hub.docker.com/_/adminer)
