start "" celery -A app.tasks.task_work:celery worker --loglevel=info -P eventlet
start "" python main.py