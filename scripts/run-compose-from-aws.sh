#!/usr/bin/env bash
set -euo pipefail

REGION="${AWS_REGION:-eu-central-1}"
DB_ID="${DB_ID:-keycloak-demo}"
DB_USERNAME_PARAMETER_NAME="${DB_USERNAME_PARAMETER_NAME:-/keycloak-demo/rds/master-username}"
KEYCLOAK_DB_PARAMETER_NAME="${KEYCLOAK_DB_PARAMETER_NAME:-/keycloak-demo/rds/db-name-keycloak}"
AUTH_DB_PARAMETER_NAME="${AUTH_DB_PARAMETER_NAME:-/keycloak-demo/rds/db-name-auth}"
APP_DB_PARAMETER_NAME="${APP_DB_PARAMETER_NAME:-/keycloak-demo/rds/db-name-app}"
KEYCLOAK_ADMIN_PASSWORD_PARAMETER_NAME="${KEYCLOAK_ADMIN_PASSWORD_PARAMETER_NAME:-/keycloak-demo/keycloak/admin-password}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

for command_name in aws sed docker; do
  if ! command -v "$command_name" >/dev/null 2>&1; then
    echo "Missing required command: $command_name" >&2
    exit 1
  fi
done

if docker compose version >/dev/null 2>&1; then
  COMPOSE_CMD=(docker compose)
elif command -v docker-compose >/dev/null 2>&1; then
  COMPOSE_CMD=(docker-compose)
else
  echo "Missing Docker Compose. Install 'docker compose' or 'docker-compose'." >&2
  exit 1
fi

get_parameter() {
  local parameter_name="$1"
  local decrypt="${2:-false}"

  if [ "$decrypt" = "true" ]; then
    aws ssm get-parameter \
      --region "$REGION" \
      --name "$parameter_name" \
      --with-decryption \
      --query "Parameter.Value" \
      --output text
  else
    aws ssm get-parameter \
      --region "$REGION" \
      --name "$parameter_name" \
      --query "Parameter.Value" \
      --output text
  fi
}

get_public_ip() {
  local token

  token="$(curl -fsX PUT "http://169.254.169.254/latest/api/token" \
    -H "X-aws-ec2-metadata-token-ttl-seconds: 21600" 2>/dev/null || true)"

  if [ -n "$token" ]; then
    curl -fs -H "X-aws-ec2-metadata-token: $token" \
      "http://169.254.169.254/latest/meta-data/public-ipv4" 2>/dev/null || true
  else
    curl -fs "http://169.254.169.254/latest/meta-data/public-ipv4" 2>/dev/null || true
  fi
}

echo "Resolving RDS and compose parameters from AWS..."

export AWS_REGION="$REGION"
export RDS_ENDPOINT="$(aws rds describe-db-instances \
  --region "$REGION" \
  --db-instance-identifier "$DB_ID" \
  --query "DBInstances[0].Endpoint.Address" \
  --output text)"
export RDS_MASTER_SECRET_ARN="$(aws rds describe-db-instances \
  --region "$REGION" \
  --db-instance-identifier "$DB_ID" \
  --query "DBInstances[0].MasterUserSecret.SecretArn" \
  --output text)"
export RDS_USERNAME="$(get_parameter "$DB_USERNAME_PARAMETER_NAME")"
export RDS_KEYCLOAK_DB="$(get_parameter "$KEYCLOAK_DB_PARAMETER_NAME")"
export RDS_AUTH_DB="$(get_parameter "$AUTH_DB_PARAMETER_NAME")"
export RDS_APP_DB="$(get_parameter "$APP_DB_PARAMETER_NAME")"
export KEYCLOAK_ADMIN_PASSWORD="$(get_parameter "$KEYCLOAK_ADMIN_PASSWORD_PARAMETER_NAME" true)"
export RDS_PASSWORD="$(aws secretsmanager get-secret-value \
  --region "$REGION" \
  --secret-id "$RDS_MASTER_SECRET_ARN" \
  --query "SecretString" \
  --output text | sed -n 's/.*"password"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p')"
export PUBLIC_HOST="${PUBLIC_HOST:-$(get_public_ip)}"

if [ -z "$RDS_ENDPOINT" ] || [ -z "$RDS_PASSWORD" ]; then
  echo "Failed to resolve RDS connection details from AWS." >&2
  exit 1
fi

cd "$REPO_ROOT"
"${COMPOSE_CMD[@]}" "$@"
