# Unity Ranking API

UnityクライアントからFastAPIサーバーへスコアを送信し、PostgreSQLに保存されたランキングを取得・表示するクライアント・サーバー構成のサンプルプロジェクトです。

このプロジェクトでは、UnityからのHTTP通信、FastAPIによるWeb API実装、PostgreSQLへのデータ保存、Docker Composeによる開発環境構築を扱います。

## 主な機能

### Unityクライアント

- ユーザー名とスコアを入力してサーバーへ送信
- サーバーからランキングを取得して表示
- スコア送信後に自分の順位を表示
- ランキング圏外の場合でも自分の順位を追加表示
- サーバー停止時など、通信に失敗した場合はエラーとして表示

### APIサーバー

- `POST /scores` でスコアを登録
- `GET /ranking` でスコア順のランキングを取得
- `GET /scores/{score_id}/rank` で指定スコアの順位を取得
- 同点のスコアは同順位として計算
- `GET /health/db` でDB接続を確認

### 開発・確認用

- Docker ComposeでFastAPI、PostgreSQL、Adminerをまとめて起動
- JSONファイルからサンプルデータを投入
- AdminerでPostgreSQLのデータを確認・編集
- Dev ContainersでFastAPI側の開発環境を利用

## 技術構成

| Area | Technology |
|---|---|
| Client | Unity 2022.3.14f1 / C# |
| API Server | FastAPI / Python |
| Database | PostgreSQL |

補助的に使っているツール:

- SQLModel: PythonコードからDBのテーブルやデータを扱いやすくするために使用
- Docker Compose: APIサーバー、PostgreSQL、Adminerをまとめて起動するために使用
- Adminer: ブラウザからPostgreSQLの中身を確認・登録・修正するために使用
- Dev Containers: VS Codeでコンテナ内の開発環境を利用するために使用

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
    - `scores.py`
  - `services/`
    - `scores.py`
  - `seed_data/`
    - `sample_scores.json`
    - `manual_test_scores.json`
- `unity-client/`
  - `ranking-client/`
    - `Assets/`
      - `Scenes/`
        - `RankingClientScene.unity`
      - `Scripts/`
        - `Api/`
        - `UI/`

## セットアップ

### 1. `.env` を作成

初回は、リポジトリ直下で `.env.example` をコピーして `.env` を作成します。



`.env.example` の内容は以下です。

```txt
POSTGRES_DB=ranking
POSTGRES_USER=postgres
POSTGRES_PASSWORD=example_password

API_HOST_PORT=8080
ADMINER_HOST_PORT=8081
```

`.env` はローカル環境用の設定ファイルなのでGit管理しません。

### 2. Docker Composeで起動

Docker Desktopを起動した状態で、リポジトリ直下から以下を実行します。

```powershell
docker compose up
```

依存関係やDockerfileを変更した後は、必要に応じて再ビルドします。

```powershell
docker compose up --build
```

起動後、以下にアクセスできます。

- FastAPI: `http://localhost:8080`
- Swagger UI: `http://localhost:8080/docs`
- Adminer: `http://localhost:8081`

## Unityクライアントの確認

Unity Hubから以下のプロジェクトを開きます。

```txt
unity-client/ranking-client
```

使用バージョン:

```txt
Unity 2022.3.14f1
```

確認手順:

1. `Assets/Scenes/RankingClientScene.unity` を開く
2. Docker ComposeでAPIサーバーを起動しておく
3. Unity EditorでPlayする
4. `Refresh` でランキングを取得する
5. UsernameとScoreを入力し、`Submit` でスコアを送信する

デフォルトのAPI接続先は以下です。

```txt
http://localhost:8080
```

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
http://localhost:8080/ranking?limit=10
```

Response:

```json
[
  {
    "username": "ore",
    "score": 12345,
    "id": 1
  },
  {
    "username": "Nova",
    "score": 9870,
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

## サンプルデータ

ランキングAPIを試しやすくするため、サンプルスコアデータを用意しています。

通常のサンプルデータを投入する場合:

```powershell
docker compose exec api python seed.py
```

既存のスコアを削除してから投入する場合:

```powershell
docker compose exec api python seed.py --clear
```

`seed.py` のデフォルトでは `server/seed_data/sample_scores.json` を使用します。

`manual_test_scores.json` は、Swagger UIやAdminerから手動でデータを追加するときの参考データです。
基本的にはCLIからまとめて投入せず、手動操作時に値をコピーして使います。

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

## PostgreSQLをCLIで確認する

PostgreSQLの対話モードに入る場合は、以下を実行します。

```powershell
docker compose exec db psql -U postgres -d ranking
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

## 今後の改善案

- APIサーバーとDBをクラウド環境へデプロイする
- Unity側のランキング機能を再利用しやすい形に整理する
- APIの自動テストを追加する
- 認証やスコア改ざん対策を検討する

## 参考ドキュメント

- [FastAPI](https://fastapi.tiangolo.com/)
- [SQLModel](https://sqlmodel.tiangolo.com/)
- [UnityWebRequest](https://docs.unity3d.com/ScriptReference/Networking.UnityWebRequest.html)
- [Docker Compose](https://docs.docker.com/compose/)
- [Dev Containers](https://code.visualstudio.com/docs/devcontainers/containers)
- [Postgres Docker Official Image](https://hub.docker.com/_/postgres)
- [Adminer Docker Official Image](https://hub.docker.com/_/adminer)
