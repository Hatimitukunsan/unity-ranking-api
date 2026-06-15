# Unity Ranking API

Unityクライアントからユーザー名とスコアを送信し、FastAPIサーバーでランキングを管理するプロジェクトです。
ランキングデータはPostgreSQLに保存し、開発環境はDocker Composeで再現できる構成にしています。

このプロジェクトでは、UnityとWeb APIの連携だけでなく、サーバー側のAPI設計、DB設計、Dockerを使った開発環境構築までを扱います。

## 技術構成

| Area | Technology |
|---|---|
| Client | Unity |
| API Server | FastAPI |
| Language | Python |
| Database | PostgreSQL |

補助的に使っているツール:

- SQLModel: PythonコードからDBのテーブルやデータを扱いやすくするために使用
- Docker Compose: APIサーバー、PostgreSQL、Adminerをまとめて起動するために使用
- Adminer: ブラウザからPostgreSQLの中身を確認・登録・修正するために使用
- Dev Containers: VS Codeでコンテナ内の開発環境を利用するために使用

## 現在できること

- ユーザー名とスコアをPOSTしてDBに登録する
- スコアの高い順にランキングをGETする
- スコアIDを指定して、そのスコアの順位をGETする（同点のスコアは同順位として扱う）
- FastAPIからPostgreSQLへ接続できるか確認する
- サンプルデータをJSONファイルからDBへ投入する
- Docker ComposeでFastAPI、PostgreSQL、Adminerをまとめて起動する

## ディレクトリ構成

- `README.md`
- `compose.yaml`
- `.env.example`
- `.devcontainer/`
  - `devcontainer.json`
  - `docker-compose.yml`
- `server/`
  - `Dockerfile`
  - `requirements.txt`
  - `main.py`
  - `database.py`
  - `models.py`
  - `seed.py`
  - `routers/`
    - `__init__.py`
    - `scores.py`
  - `services/`
    - `__init__.py`
    - `scores.py`
  - `seed_data/`
    - `sample_scores.json`
    - `manual_test_scores.json`
- `unity-client/`

## 環境変数

このプロジェクトでは、ローカル環境ごとに変わる値を `.env` に書きます。
`.env` はGit管理せず、見本として `.env.example` を管理します。

初回は、`.env.example` をコピーして `.env` を作成してください。

```powershell
copy .env.example .env
```

`.env.example` の内容は以下です。

```txt
POSTGRES_DB=ranking
POSTGRES_USER=postgres
POSTGRES_PASSWORD=example_password

API_HOST_PORT=8080
ADMINER_HOST_PORT=8081
```

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

```powershell
docker compose up
```

起動後、ブラウザで以下にアクセスできます。

- FastAPI: `http://localhost:8080`
- FastAPI Swagger UI: `http://localhost:8080/docs`
- Adminer: `http://localhost:8081`

## API一覧

### `GET /`

動作確認用のエンドポイントです。

Response:

```json
{
  "message": "Hello World"
}
```

### `GET /health/db`

FastAPIからPostgreSQLへ接続できるか確認します。

Response:

```json
{
  "database": "ok"
}
```

### `POST /scores`

ユーザー名とスコアを登録します。

Request:

```json
{
  "username": "test_player",
  "score": 1234
}
```

Response:

```json
{
  "username": "test_player",
  "score": 1234,
  "id": 1
}
```

### `GET /ranking`

スコアの高い順にランキングを取得します。
`limit` を指定しない場合は100件、最大500件まで取得できます。

Example:

```txt
http://localhost:8080/ranking?limit=30
```

Response:

```json
[
  {
    "username": "top_player",
    "score": 3000,
    "id": 1
  },
  {
    "username": "challenger_a",
    "score": 2850,
    "id": 2
  }
]
```

### `GET /scores/{score_id}/rank`

スコアIDを指定して、そのスコアの順位を取得します。
同点のスコアは同順位として扱います。

Example:

```txt
http://localhost:8080/scores/1/rank
```

Response:

```json
{
  "username": "test_player",
  "score": 1234,
  "id": 1,
  "rank": 10
}
```

## サンプルデータ投入

ランキングAPIを試しやすくするため、サンプルスコアデータを用意しています。

通常のサンプルデータを投入する場合:

```powershell
docker compose exec api python seed.py
```

既存のスコアを削除してから投入する場合:

```powershell
docker compose exec api python seed.py --clear
```

`manual_test_scores.json` は、Swagger UIやAdminerから手動でデータを追加するときの参考データです。
必要に応じて、まとめて投入することもできます。

手動確認用のデータをまとめて投入する場合:

```powershell
docker compose exec api python seed.py seed_data/manual_test_scores.json --clear
```

## Adminerの接続情報

Adminerでは、ブラウザからPostgreSQLのデータを確認できます。
開発中の動作確認として、スコアデータの確認、登録、修正、削除を行うこともできます。

`.env` をデフォルト値にしている場合、以下の内容でPostgreSQLに接続できます。

```txt
System: PostgreSQL
Server: db
Username: postgres
Password: example_password
Database: ranking
```

## PostgreSQLの確認

PostgreSQLの対話モードに入るために、以下のように `psql` を実行できます。

```powershell
docker compose exec db psql -U postgres -d ranking
```

## 停止方法

コンテナを停止する場合は以下を実行します。

```powershell
docker compose down
```

PostgreSQLのデータは名前付きvolume `postgres-data` に保存されるため、通常の `docker compose down` では削除されません。

データも含めて削除したい場合のみ、以下を実行します。

```powershell
docker compose down -v
```

## 今後の作業

1. UnityからHTTP通信

   Unityの `UnityWebRequest` を使い、スコア送信とランキング取得を行います。

2. 入力値のバリデーション強化

   ユーザー名の長さやスコアの範囲など、APIとして受け付ける値の条件を整理します。

3. テスト追加

   登録、ランキング取得、順位取得の動作を自動テストで確認できるようにします。

4. 簡単な不正対策

   クライアントから送られるスコアをそのまま信用しすぎないよう、最低限の対策を検討します。

## 参考ドキュメント

- [FastAPI](https://fastapi.tiangolo.com/)
- [SQLModel](https://sqlmodel.tiangolo.com/)
- [Docker Compose](https://docs.docker.com/compose/)
- [Dev Containers](https://code.visualstudio.com/docs/devcontainers/containers)
- [Postgres Docker Official Image](https://hub.docker.com/_/postgres)
- [Adminer Docker Official Image](https://hub.docker.com/_/adminer)
