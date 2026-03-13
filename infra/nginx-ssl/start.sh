#!/bin/sh
set -eu

PUBLIC_HOST="${PUBLIC_HOST:-localhost}"
CERT_DIR="/etc/nginx/certs"

mkdir -p "$CERT_DIR"

openssl req \
  -x509 \
  -nodes \
  -newkey rsa:2048 \
  -days 365 \
  -keyout "$CERT_DIR/server.key" \
  -out "$CERT_DIR/server.crt" \
  -subj "/CN=$PUBLIC_HOST" \
  -addext "subjectAltName=IP:$PUBLIC_HOST" >/dev/null 2>&1

envsubst '${PUBLIC_HOST}' < /etc/nginx/templates/default.conf.template > /etc/nginx/conf.d/default.conf

exec nginx -g 'daemon off;'
