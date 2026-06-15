from typing import Annotated

from fastapi import APIRouter, HTTPException, Query
from sqlalchemy import func, text
from sqlmodel import select

from database import SessionDep
from models import Score, ScoreCreate, ScorePublic, ScoreRank

router = APIRouter()


@router.get("/health/db")
def health_db(session: SessionDep):
    # 軽いSQLを実行して、FastAPIからPostgreSQLへ接続できるか確認する
    session.exec(text("SELECT 1"))
    return {"database": "ok"}


@router.post("/scores", response_model=ScorePublic)
def create_score(score_create: ScoreCreate, session: SessionDep):
    # リクエストBodyをDB保存用モデルに変換して登録する
    score = Score.model_validate(score_create)
    session.add(score)
    session.commit()
    # commit後にDB側で決まったidなどをPythonオブジェクトへ反映する
    session.refresh(score)
    return score


@router.get("/ranking", response_model=list[ScorePublic])
def read_ranking(
    session: SessionDep,
    limit: Annotated[int, Query(ge=1, le=500)] = 100,
):
    # スコアの高い順に並べ、指定件数だけ取得する
    statement = select(Score).order_by(Score.score.desc()).limit(limit)
    scores = session.exec(statement).all()
    return scores


@router.get("/scores/{score_id}/rank", response_model=ScoreRank)
def read_score_rank(score_id: int, session: SessionDep):
    # 指定されたIDのスコアを取得する
    score = session.get(Score, score_id)
    if score is None:
        raise HTTPException(status_code=404, detail="Score not found")

    # 同点は同順位にするため、自分より高いスコアだけを数える
    higher_score_count = session.exec(
        select(func.count()).select_from(Score).where(Score.score > score.score)
    ).one()
    my_rank = higher_score_count + 1

    return ScoreRank.model_validate(score, update={"rank": my_rank})
