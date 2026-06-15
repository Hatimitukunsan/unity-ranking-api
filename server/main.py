from contextlib import asynccontextmanager

from fastapi import FastAPI

from database import create_db_and_tables
from routers import scores


@asynccontextmanager
async def lifespan(app: FastAPI):
    # アプリがリクエストを受け付ける前にテーブルを準備する
    create_db_and_tables()
    yield


app = FastAPI(lifespan=lifespan)
app.include_router(scores.router)


@app.get("/")
async def root():
    return {"message": "Hello World"}
