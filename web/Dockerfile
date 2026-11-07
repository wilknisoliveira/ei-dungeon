FROM node:22-alpine AS build

WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY angular.json tsconfig.json tsconfig.app.json tsconfig.spec.json ./
COPY scripts/ ./scripts/
COPY src/ ./src/

ARG SITE_URL
ARG API_URL
ENV SITE_URL=${SITE_URL}
ENV API_URL=${API_URL}

RUN npm run build

FROM nginx:alpine AS runtime

COPY nginx.conf /etc/nginx/nginx.conf
COPY --from=build /app/dist/ei-dungeon-web/browser/ /usr/share/nginx/html/

RUN nginx -t

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://127.0.0.1/en/ || exit 1
