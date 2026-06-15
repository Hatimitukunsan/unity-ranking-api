from typing import Annotated

from fastapi import APIRouter, HTTPException, Query
from sqlalchemy import text
from sqlmodel import select

from database import SessionDep
from models import Score, ScoreCreate, ScorePublic, ScoreRank
from services.scores import calculate_score_rank, create_score_record

router = APIRouter()


@router.get("/health/db")
def health_db(session: SessionDep):
    # 軽いSQLを実行して、FastAPIからPostgreSQLへ接続できるか確認する
    session.exec(text("SELECT 1"))
    return {"database": "ok"}


@router.post("/scores", response_model=ScorePublic)
def create_score(score_create: ScoreCreate, session: SessionDep):
    return create_score_record(session, score_create)


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

    rank = calculate_score_rank(session, score)
    return ScoreRank.model_validate(score, update={"rank": rank})
