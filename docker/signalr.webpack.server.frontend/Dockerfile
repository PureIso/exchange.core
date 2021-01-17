#Angular build
FROM node as base
# set working directory
RUN mkdir /usr/src/app
WORKDIR /usr/src/app
# add `/usr/src/app/node_modules/.bin` to $PATH
ENV PATH /usr/src/app/node_modules/.bin:$PATH
# install and cache app dependencies
COPY ["exchange.signalR.client.web.frontend/package.json", "exchange.signalR.client.web.frontend/package.json"]
COPY exchange.signalR.client.web.frontend/. /usr/src/app/exchange.signalR.client.web.frontend/
WORKDIR /usr/src/app/exchange.signalR.client.web.frontend/
RUN npm install
RUN npm audit fix
# RUN npm test
# RUN npm run e2e