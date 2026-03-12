FROM debian:bookworm-slim AS awscli

RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl unzip \
    && curl -fsSL "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" -o "/tmp/awscliv2.zip" \
    && unzip -q /tmp/awscliv2.zip -d /tmp \
    && /tmp/aws/install \
    && rm -rf /tmp/aws /tmp/awscliv2.zip /var/lib/apt/lists/*

FROM quay.io/keycloak/keycloak:latest

USER root

COPY --from=awscli /usr/local/aws-cli /usr/local/aws-cli
COPY --from=awscli /usr/local/bin/aws /usr/local/bin/aws

COPY keycloak/start-keycloak.sh /opt/keycloak/start-keycloak.sh
RUN chmod +x /opt/keycloak/start-keycloak.sh

USER 1000

ENTRYPOINT ["/opt/keycloak/start-keycloak.sh"]
