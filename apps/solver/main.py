"""
Jobuler Solver Service
Entry point for the FastAPI application.
"""
from fastapi import FastAPI
from routers import solve, health

app = FastAPI(title="Jobuler Solver", version="0.1.0")

app.include_router(health.router)
app.include_router(solve.router)
