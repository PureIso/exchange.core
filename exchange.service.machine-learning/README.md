# Deep Learning

Time Series forcasting with Tensorflow using Supervised Learning.
Sequential Data
Recurrent Neural Network (RNN - layer)
Long Short Term Memory (LSTM - cells)

## Prerequisites

Make sure that you have: python3.6.5
Task Queue: Celery
Message Broker for Task Queue: rabbitmq
Dependancy: Erlang

Upgrade pip

```shell
python -m pip install --upgrade pip
```

Make sure you have virtualenv installed
Virtualenv allows you to have different virtual environment for different projects.

```shell
pip install virtualenv
```

Setup and initialise virtual environment:

```shell
virtualenv env --python=python3.6.5
python -m virtualenv env
env\\Scripts\\activate.bat
python.exe -m pip install --upgrade pip
```

Installing requirement file in virtual environment:

```shell
pip install -r requirements.txt
```

Backing up packages to requirement file:

```shell
pip freeze --local > requirements.txt
```

Start Celery:

```shell
celery -A main worker --loglevel=info
```