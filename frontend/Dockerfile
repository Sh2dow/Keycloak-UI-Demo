FROM node:22-alpine AS build
WORKDIR /app

ARG VITE_KEYCLOAK_AUTHORITY
ENV VITE_KEYCLOAK_AUTHORITY=${VITE_KEYCLOAK_AUTHORITY}

COPY frontend/package.json frontend/bun.lock ./
RUN npm install

COPY frontend/ ./
RUN npm run build

FROM nginx:1.27-alpine AS final
WORKDIR /usr/share/nginx/html

COPY frontend/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist ./

EXPOSE 80
