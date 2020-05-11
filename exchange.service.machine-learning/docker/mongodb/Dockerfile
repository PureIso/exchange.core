FROM mongo:4.2

COPY mongo-init.js /docker-entrypoint-initdb.d/

RUN ["chmod", "+x", "/docker-entrypoint-initdb.d"]