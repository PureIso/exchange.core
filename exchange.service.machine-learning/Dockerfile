FROM ubuntu:18.04

WORKDIR /usr/src/app

COPY requirements.txt ./

RUN apt-get update && apt-get -y install python3.6 && apt-get -y install python3-pip &&  apt-get update  && python3.6 -m pip install --upgrade pip && python3.6 -m pip install -r requirements.txt

COPY . .
CMD ["python3.6", "main.py"]

EXPOSE 5005