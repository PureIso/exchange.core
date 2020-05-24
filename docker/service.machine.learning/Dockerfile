FROM ubuntu:18.04 as base
# set working directory
RUN mkdir /usr/src/app
RUN chmod 775 /usr/src/app
WORKDIR /usr/src/app
COPY exchange.service.machine.learning/ /usr/src/app/
RUN apt-get update && apt-get -y install python3.6 && apt-get -y install python3-pip &&  apt-get update && apt-get install -y rsync && python3.6 -m pip install --upgrade pip && python3.6 -m pip install -r requirements.txt