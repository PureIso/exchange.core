# Deep Learning

Time Series forcasting with Tensorflow using Supervised Learning. \
Sequential Data \
Recurrent Neural Network (RNN - layer) \
Long Short Term Memory (LSTM - cells)

## Prerequisites

Make sure that you have:

- python3.6.5
- Task Queue: Celery
- Message Broker for Task Queue: mongoDB
- Database: mongoDB
- Dependancy: Erlang


### Quick Start - Docker-Compose (Start Flask RESTful, Celery and MongoDB)

Running docker-compose:

```shell
docker-compose up
```

View output of docker-compose:

```shell
docker-compose -f docker-compose.yml config
docker-compose down -v
docker-compose down -v && docker-compose up --build
```

### Quick Start - Docker Commands

Build docker file:

```shell
docker build -t exchange.service.machine-learning-service:latest .
```

Creating a volume:

```shell
docker volume create --driver local --opt type=none --opt device=D:\Docker\data\db --opt o=bind mongo_db
docker volume create --driver local --opt type=none --opt device=D:\Docker\data\configdb --opt o=bind mongo_configdb
```

Clean dangling images:

```shell
docker image prune
```

### Quick Start - Local

[Microsoft Visual C++](https://visualstudio.microsoft.com/visual-cpp-build-tools/)
Upgrade pip

```shell
python -m pip install --upgrade pip
```

Make sure you have virtualenv installed
Virtualenv allows you to have different virtual environment for different projects.

```shell
pip install virtualenv 
```

Get the current Python version.

```shell
python --version   
```

NOTE: TensorFlow Supports python3.8.6 x64 or python3.6.5 x86
Setup and initialise virtual environment with the correct python version:

```shell
virtualenv env --python=python3.8.6
python -m virtualenv env
env\\Scripts\\activate.bat
python.exe -m pip install --upgrade pip
```

Installing requirement file in virtual environment:

```shell
pip install -r requirements.txt
```

Backing up packages to requirement file especially after tested update:

```shell
pip freeze --local > requirements.txt
```

### Quick Start - Env (Start Flask RESTful and Celery)

Initialise environment and Start application:

```shell
activate.bat
start.bat
```