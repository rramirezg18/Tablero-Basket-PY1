# ===== build (Angular) =====
FROM node:20-alpine AS build
WORKDIR /app

# Solo package*.json para cache
COPY scoreboard/package*.json ./scoreboard/
WORKDIR /app/scoreboard
RUN npm ci || npm install

# Código
COPY scoreboard/ .

# Build Angular (produce dist/scoreboard/browser)
RUN npx -y -p @angular/cli@20 ng build -c production


# ===== runtime Nginx =====
FROM nginx:alpine
WORKDIR /usr/share/nginx/html

# Elimina el index por defecto de Nginx
RUN rm -rf ./*

# OJO: copiamos el contenido de /browser al root de Nginx
# (comentario en su propia línea; NO al final de COPY)
COPY --from=build /app/scoreboard/dist/scoreboard/browser/ ./

# Config de Nginx (SPA + proxy a /api)
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
