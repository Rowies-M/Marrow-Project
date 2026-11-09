# Contexto de build: frontend/marrow-web

# 1. Build: instala dependencias y genera los archivos estáticos.
FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci

# Vite fija esta URL en los archivos generados, así que se indica al construir la imagen.
# Es pública en el navegador: nunca pases secretos por aquí.
ARG VITE_API_URL
RUN test -n "$VITE_API_URL" || (echo "Falta indicar --build-arg VITE_API_URL=<URL de la API>" && exit 1)
COPY . .
RUN npm run build

# 2. Ejecución: solo nginx y los archivos generados, sin Node ni código fuente.
FROM nginxinc/nginx-unprivileged:stable-alpine AS final
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 8080
