#!/usr/bin/env bash
set -euo pipefail

realm_name="${KEYCLOAK_REALM_NAME:-myrealm}"
frontend_client_id="${KEYCLOAK_FRONTEND_CLIENT_ID:-react-client}"
app_public_url="${APP_PUBLIC_URL:-}"
admin_user="${KEYCLOAK_ADMIN:-admin}"
admin_password="${KEYCLOAK_ADMIN_PASSWORD:-}"

if [ -z "$admin_password" ]; then
  echo "Missing required environment variable: KEYCLOAK_ADMIN_PASSWORD" >&2
  exit 1
fi

/opt/keycloak/bin/kc.sh start-dev &
kc_pid=$!

cleanup() {
  if kill -0 "$kc_pid" >/dev/null 2>&1; then
    kill "$kc_pid" >/dev/null 2>&1 || true
  fi
}

trap cleanup INT TERM

wait_for_keycloak() {
  local attempts=60

  until curl -fsS http://127.0.0.1:8080/realms/master >/dev/null 2>&1; do
    attempts=$((attempts - 1))
    if [ "$attempts" -le 0 ]; then
      echo "Keycloak did not become ready in time." >&2
      return 1
    fi

    sleep 2
  done
}

ensure_realm() {
  if /opt/keycloak/bin/kcadm.sh get "realms/${realm_name}" >/dev/null 2>&1; then
    return 0
  fi

  /opt/keycloak/bin/kcadm.sh create realms -s realm="${realm_name}" -s enabled=true >/dev/null
}

ensure_frontend_client() {
  if [ -z "$app_public_url" ]; then
    echo "APP_PUBLIC_URL is not set. Skipping Keycloak client redirect/origin sync." >&2
    return 0
  fi

  local redirect_uri="${app_public_url%/}/*"
  local client_lookup
  client_lookup="$(/opt/keycloak/bin/kcadm.sh get "clients?clientId=${frontend_client_id}" -r "${realm_name}")"

  local existing_client_id
  existing_client_id="$(printf '%s' "$client_lookup" | sed -n 's/.*"id"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | head -n 1)"

  if [ -z "$existing_client_id" ]; then
    /opt/keycloak/bin/kcadm.sh create clients -r "${realm_name}" \
      -s clientId="${frontend_client_id}" \
      -s enabled=true \
      -s publicClient=true \
      -s standardFlowEnabled=true \
      -s directAccessGrantsEnabled=true \
      -s rootUrl="${app_public_url}" \
      -s 'redirectUris=["'"${redirect_uri}"'"]' \
      -s 'webOrigins=["'"${app_public_url}"'"]' >/dev/null
    return 0
  fi

  /opt/keycloak/bin/kcadm.sh update "clients/${existing_client_id}" -r "${realm_name}" \
    -s enabled=true \
    -s publicClient=true \
    -s standardFlowEnabled=true \
    -s directAccessGrantsEnabled=true \
    -s rootUrl="${app_public_url}" \
    -s 'redirectUris=["'"${redirect_uri}"'"]' \
    -s 'webOrigins=["'"${app_public_url}"'"]' >/dev/null
}

wait_for_keycloak
/opt/keycloak/bin/kcadm.sh config credentials \
  --server http://127.0.0.1:8080 \
  --realm master \
  --user "${admin_user}" \
  --password "${admin_password}" >/dev/null

ensure_realm
ensure_frontend_client

wait "$kc_pid"
