@echo off
start "Exchange Core ML Worker" .venv\Scripts\python.exe -m celery -A app.tasks.task_work:celery worker --loglevel=INFO --pool=solo
start "Exchange Core ML API" .venv\Scripts\python.exe main.py
