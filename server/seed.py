import argparse
import json
from pathlib import Path

from sqlmodel import Session, delete

from database import create_db_and_tables, engine
from models import Score, ScoreCreate
from services.scores import create_score_record

DEFAULT_DATA_FILE = Path(__file__).parent / "seed_data" / "sample_scores.json"


def load_score_data(path: Path) -> list[ScoreCreate]:
    # JSONファイルを読み込み、APIのPOSTと同じ入力用モデルとして扱う
    with path.open(encoding="utf-8") as file:
        raw_scores = json.load(file)

    if not isinstance(raw_scores, list):
        raise ValueError("シードデータはスコア情報の配列である必要があります")

    return [ScoreCreate.model_validate(raw_score) for raw_score in raw_scores]


def seed_scores(data_file: Path, clear_existing: bool) -> int:
    # テーブル作成後、指定されたJSONファイルのスコアをDBへ登録する
    scores = load_score_data(data_file)
    create_db_and_tables()

    with Session(engine) as session:
        if clear_existing:
            # 動作確認をやり直しやすいように、必要に応じて既存データを消す
            session.exec(delete(Score))
            session.commit()

        for score_create in scores:
            create_score_record(session, score_create)

    return len(scores)


def parse_args():
    parser = argparse.ArgumentParser(description="ランキング用のサンプルスコアをDBに登録します")
    parser.add_argument(
        "data_file",
        nargs="?",
        type=Path,
        default=DEFAULT_DATA_FILE,
        help="登録するスコアデータのJSONファイルパス",
    )
    parser.add_argument(
        "--clear",
        action="store_true",
        help="登録前に既存のスコアデータを削除する",
    )
    return parser.parse_args()


def main():
    args = parse_args()
    data_file = args.data_file
    if not data_file.is_absolute():
        # 相対パスで指定された場合は、コマンド実行場所からのパスとして解釈する
        data_file = Path.cwd() / data_file

    inserted_count = seed_scores(data_file, args.clear)
    print(f"{inserted_count}件のスコアデータを登録しました: {data_file}")


if __name__ == "__main__":
    main()
