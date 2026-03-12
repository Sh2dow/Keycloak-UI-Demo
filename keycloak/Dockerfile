FROM quay.io/keycloak/keycloak:latest

USER root

RUN microdnf install -y python3 curl unzip \
    && curl -fsSL "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" -o "/tmp/awscliv2.zip" \
    && cd /tmp \
    && unzip -q awscliv2.zip \
    && /tmp/aws/install \
    && rm -rf /tmp/aws /tmp/awscliv2.zip \
    && microdnf clean all

COPY keycloak/start-keycloak.sh /opt/keycloak/start-keycloak.sh
RUN chmod +x /opt/keycloak/start-keycloak.sh

USER 1000

ENTRYPOINT ["/opt/keycloak/start-keycloak.sh"]
