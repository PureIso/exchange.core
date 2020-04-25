# stage 1
# image optimised for building the application
# build the image to generate a dist folder which contains all the deployable files
FROM node:latest as node
WORKDIR /app
COPY . .
RUN npm install
RUN npm run build:dist

# stage 2
# image optimised for production for running the application
# run the application
FROM nginx:alpine
COPY --from=node /app/dist /usr/share/nginx/html
