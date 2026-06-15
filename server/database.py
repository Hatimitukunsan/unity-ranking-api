import os
from typing import Annotated

from fastapi import Depends
from sqlmodel import SQLModel, Session, create_engine

# DB接続情報はDocker Composeから環境変数として渡す
database_url = os.environ["DATABASE_URL"]
engine = create_engine(database_url)


def create_db_and_tables():
    # モデルを読み込んでから、未作成のテーブルを作る
    # モデルを読み込むため下の行は削除してはならない
    import models  # noqa: F401

    SQLModel.metadata.create_all(engine)


def get_session():
    # APIごとにDBセッションを作り、処理後に自動で閉じる
    with Session(engine) as session:
        yield session


# FastAPIのDependencyとしてDBセッションを受け取るための型
SessionDep = Annotated[Session, Depends(get_session)]
