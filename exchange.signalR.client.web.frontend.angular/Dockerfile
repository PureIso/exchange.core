FROM node:24.19.0-alpine AS build

WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:1.30.4-alpine3.24-slim AS runtime

COPY nginx/nginx.conf /etc/nginx/nginx.conf
COPY nginx/default.conf /etc/nginx/conf.d/default.conf
COPY nginx/write-runtime-config.sh /usr/local/bin/write-runtime-config
RUN chmod +x /usr/local/bin/write-runtime-config
COPY --from=build /app/dist/exchange-web/browser /usr/share/nginx/html

ENV EXCHANGE_HUB_URL=http://localhost:5000/hubs/exchange
ENV ML_API_URL=http://localhost:5005/api/v1/
USER nginx
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD wget -qO- http://127.0.0.1:8080/ >/dev/null || exit 1

ENTRYPOINT ["/usr/local/bin/write-runtime-config"]
