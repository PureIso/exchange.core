# syntax=docker/dockerfile:1.7

FROM python:3.13.14-slim-bookworm AS dependencies

ENV PIP_DISABLE_PIP_VERSION_CHECK=1 \
    PIP_NO_CACHE_DIR=1 \
    PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1

WORKDIR /build
RUN python -m venv /opt/venv
ENV PATH="/opt/venv/bin:${PATH}"

COPY exchange.service.machine.learning/requirements.lock ./requirements.lock
RUN python -m pip install --require-hashes --no-deps -r requirements.lock

FROM python:3.13.14-slim-bookworm AS runtime

ENV PATH="/opt/venv/bin:${PATH}" \
    PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    TF_CPP_MIN_LOG_LEVEL=2

RUN apt-get update \
    && apt-get install --yes --no-install-recommends libgomp1 \
    && rm -rf /var/lib/apt/lists/* \
    && groupadd --gid 10001 machinelearning \
    && useradd --uid 10001 --gid machinelearning --no-create-home --shell /usr/sbin/nologin machinelearning

WORKDIR /usr/src/app
COPY --from=dependencies /opt/venv /opt/venv
COPY --chown=machinelearning:machinelearning exchange.service.machine.learning/ ./
RUN mkdir -p /usr/src/app/static \
    && chown machinelearning:machinelearning /usr/src/app/static

USER 10001:10001
EXPOSE 5005

HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD ["python", "-c", "import urllib.request; urllib.request.urlopen('http://127.0.0.1:5005/api/v1/predict/', timeout=4).read()"]

CMD ["python", "main.py"]
