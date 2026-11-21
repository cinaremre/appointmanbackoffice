# React backoffice - node derleme + nginx statik sunum.
# /api istekleri nginx tarafindan api container'ina proxy'lenir; token
# yalnizca istemci ile api arasinda tasinir, dis URL set edilmez.
FROM node:22-alpine AS build
WORKDIR /app

COPY BackOffice/package.json BackOffice/package-lock.json ./
RUN npm ci

COPY BackOffice/ .
RUN npm run build

FROM nginx:alpine AS serve
COPY BackOffice/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80
