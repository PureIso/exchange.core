# Machine-Learning Service

The required Flask/Celery service trains and loads TensorFlow/Keras LSTM models for five-observation time-series prediction. See the modernization record in `06-exchange-core/11-machine-learning-characterization.md` in the companion documentation vault.

## Supported runtime

- Python 3.13
- TensorFlow 2.21 / Keras 3.15
- Flask 3.1
- Celery 5.6 with MongoDB broker/result backend
- `requirements.in` for intentional direct dependencies
- `requirements.lock` for the generated Python 3.13/Linux hash lock

The container uses the patch-pinned official `python:3.13.14-slim-bookworm` image. It installs the lock with hash enforcement, runs as UID/GID `10001`, writes only to the mounted `static` artifact directory and `/tmp`, and provides web and worker health checks through Compose.

## Local Windows setup

Install `uv`, copy `.env.example` to `.env`, and supply deployment-specific values without committing the file. Then run:

```powershell
./init.bat
./start.bat
```

The local worker uses Celery's `solo` pool because Celery does not officially support Windows. The Linux container uses the default prefork pool.

Run deterministic tests against the locked environment:

```powershell
uv run --no-project --python 3.13 --with-requirements requirements.txt python -m unittest discover -s tests -v
```

Regenerate the Linux lock only after reviewing direct dependency changes:

```powershell
uv pip compile requirements.in --python-version 3.13 --python-platform x86_64-manylinux_2_28 --generate-hashes --no-emit-index-url --output-file requirements.lock
```

## Docker Compose

From the repository root, copy `.env.example` to `.env`, populate all required values, and run:

```powershell
docker compose config
docker compose up --build mongodb celery machinelearning
```

The `machinelearning` service serves port `5005`. The `celery` service uses the same image with an explicit worker command. Both share `/usr/src/app/static` for CSV, normalizer, and H5 artifacts.

Build the image directly from the repository root with:

```powershell
docker build -f docker/service.machine.learning/Dockerfile -t exchange-core-machine-learning:local .
```

## API

- `GET /` and `GET /api/v1/rnn/`: API version and available CSV indicator files.
- `POST /api/v1/rnn/`: dispatch training with JSON `indicator_file`.
- `GET /api/v1/predict/`: API version.
- `POST /api/v1/predict/`: dispatch prediction with JSON `indicator_file`.
- `POST /api/v1/taskstatus/`: inspect a Celery task with JSON `task_id`.

Current request/response schemas and remaining acceptance gates are documented in the characterization record. Do not enable model retraining or production deployment until the `ML-02C` real-artifact, Celery, and container smoke gates pass.
