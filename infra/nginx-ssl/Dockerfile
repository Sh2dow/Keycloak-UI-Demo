FROM nginx:1.27-alpine

RUN apk add --no-cache openssl bash gettext

COPY infra/nginx-ssl/default.conf.template /etc/nginx/templates/default.conf.template
COPY infra/nginx-ssl/start.sh /start.sh

RUN chmod +x /start.sh

ENTRYPOINT ["/start.sh"]
