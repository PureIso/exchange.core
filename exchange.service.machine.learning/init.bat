@echo off
uv venv --python 3.13 .venv
uv pip sync --python .venv\Scripts\python.exe requirements.lock
