from sqlmodel import Field, SQLModel


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


# 順位確認APIのレスポンスとして返すデータ
class ScoreRank(ScorePublic):
    rank: int
