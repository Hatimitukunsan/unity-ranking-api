import os

from fastapi import FastAPI
from sqlalchemy import text
from sqlmodel import Session, create_engine

app = FastAPI()

database_url = os.environ["DATABASE_URL"]
engine = create_engine(database_url)


@app.get("/")
async def root():
    return {"message": "Hello World"}


@app.get("/health/db")
def health_db():
    with Session(engine) as session:
        session.exec(text("SELECT 1"))
    return {"database": "ok"}
