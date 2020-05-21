FROM mcr.microsoft.com/dotnet/core/sdk:5.0-buster-slim AS base
WORKDIR /app

#Angular build
FROM node as nodebuilder

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
#RUN npm run test
RUN npm run build:dist
#End Angular build

FROM mcr.microsoft.com/dotnet/core/sdk:5.0-buster AS build
WORKDIR /src
COPY ["exchange.signalR.client.web.backend/exchange.signalR.client.web.backend.csproj", "exchange.signalR.client.web.backend/"]
COPY --from=nodebuilder  /usr/src/app/exchange.signalR.client.web.backend/wwwroot/ ./src/exchange.signalR.client.web.backend/wwwroot/

RUN apt-get update && apt-get install -y nodejs
RUN dotnet restore "exchange.signalR.client.web.backend/exchange.signalR.client.web.backend.csproj"
COPY . .
WORKDIR "/src/exchange.signalR.client.web.backend"
RUN dotnet test "exchange.signalR.client.web.backend.csproj" -c Release
WORKDIR "/src/exchange.signalR.client.web.backend"
RUN dotnet publish "exchange.signalR.client.web.backend.csproj" -c Release -o /app/publish

FROM base AS final
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet","exchange.signalR.client.web.backend.dll"]