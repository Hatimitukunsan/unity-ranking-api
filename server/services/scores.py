from sqlalchemy import func
from sqlmodel import Session, select

from models import Score, ScoreCreate


def create_score_record(session: Session, score_create: ScoreCreate) -> Score:
    # 入力用モデルをDB保存用モデルに変換して登録する
    score = Score.model_validate(score_create)
    session.add(score)
    session.commit()
    # commit後にDB側で決まったidをPythonオブジェクトへ反映する
    session.refresh(score)
    return score


def calculate_score_rank(session: Session, score: Score) -> int:
    # 同点は同順位にするため、自分より高いスコアだけを数える
    higher_score_count = session.exec(
        select(func.count()).select_from(Score).where(Score.score > score.score)
    ).one()
    return higher_score_count + 1
