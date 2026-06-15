import os
from contextlib import asynccontextmanager
from typing import Annotated

from fastapi import Depends, FastAPI, Query
from sqlalchemy import text
from sqlmodel import Field, SQLModel, Session, create_engine, select

# DB接続情報はDocker Composeから環境変数として渡す
database_url = os.environ["DATABASE_URL"]
engine = create_engine(database_url)


# ランキングAPIで共通して使う基本データ
class ScoreBase(SQLModel):
    username: str
    score: int


# DBに保存するランキング用テーブル
class Score(ScoreBase, table=True):
    id: int | None = Field(default=None, primary_key=True)


# POST /scores のリクエストBodyとして受け取るデータ
class ScoreCreate(ScoreBase):
    pass


# APIレスポンスとして返すデータ
class ScorePublic(ScoreBase):
    id: int


def create_db_and_tables():
    # 学習用の最小構成として、起動時に未作成のテーブルを作る
    SQLModel.metadata.create_all(engine)


@asynccontextmanager
async def lifespan(app: FastAPI):
    # アプリがリクエストを受け付ける前にテーブルを準備する
    create_db_and_tables()
    yield


app = FastAPI(lifespan=lifespan)


def get_session():
    # APIごとにDBセッションを作り、処理後に自動で閉じる
    with Session(engine) as session:
        yield session


# FastAPIのDependencyとしてDBセッションを受け取るための型
SessionDep = Annotated[Session, Depends(get_session)]


@app.get("/")
async def root():
    return {"message": "Hello World"}


@app.get("/health/db")
def health_db(session: SessionDep):
    # 軽いSQLを実行して、FastAPIからPostgreSQLへ接続できるか確認する
    session.exec(text("SELECT 1"))
    return {"database": "ok"}


@app.post("/scores", response_model=ScorePublic)
def create_score(score_create: ScoreCreate, session: SessionDep):
    # リクエストBodyをDB保存用モデルに変換して登録する
    score = Score.model_validate(score_create)
    session.add(score)
    session.commit()
    # commit後にDB側で決まったidなどをPythonオブジェクトへ反映する
    session.refresh(score)
    return score


@app.get("/ranking", response_model=list[ScorePublic])
def read_ranking(
    session: SessionDep,
    limit: Annotated[int, Query(ge=1, le=500)] = 100,
):
    # スコアの高い順に並べ、指定件数だけ取得する
    statement = select(Score).order_by(Score.score.desc()).limit(limit)
    scores = session.exec(statement).all()
    return scores
