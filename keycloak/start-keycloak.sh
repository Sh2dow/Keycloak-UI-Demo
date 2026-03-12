#!/usr/bin/env bash
set -euo pipefail

required_vars=(
  AWS_REGION
  RDS_ENDPOINT
  RDS_USERNAME
  RDS_KEYCLOAK_DB
  RDS_MASTER_SECRET_ARN
  KEYCLOAK_ADMIN_PASSWORD_PARAMETER_NAME
)

for name in "${required_vars[@]}"; do
  if [ -z "${!name:-}" ]; then
    echo "Missing required environment variable: $name" >&2
    exit 1
  fi
done

RDS_PASSWORD="$(aws secretsmanager get-secret-value \
  --region "$AWS_REGION" \
  --secret-id "$RDS_MASTER_SECRET_ARN" \
  --query 'SecretString' \
  --output text | sed -n 's/.*"password"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p')"

KEYCLOAK_ADMIN_PASSWORD="$(aws ssm get-parameter \
  --region "$AWS_REGION" \
  --name "$KEYCLOAK_ADMIN_PASSWORD_PARAMETER_NAME" \
  --with-decryption \
  --query 'Parameter.Value' \
  --output text)"

export KC_DB=postgres
export KC_DB_URL="jdbc:postgresql://${RDS_ENDPOINT}:5432/${RDS_KEYCLOAK_DB}?sslmode=require"
export KC_DB_USERNAME="${RDS_USERNAME}"
export KC_DB_PASSWORD="${RDS_PASSWORD}"
export KEYCLOAK_ADMIN=admin
export KEYCLOAK_ADMIN_PASSWORD
export KC_HOSTNAME_STRICT=false
export KC_HTTP_ENABLED=true

exec /opt/keycloak/bin/kc.sh start-dev
