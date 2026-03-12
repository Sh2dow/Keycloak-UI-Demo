FROM public.ecr.aws/aws-cli/aws-cli:2 AS awscli

FROM quay.io/keycloak/keycloak:latest

USER root

COPY --from=awscli /usr/local/aws-cli /usr/local/aws-cli
COPY --from=awscli /usr/local/bin/aws /usr/local/bin/aws

RUN microdnf install -y python3 \
    && microdnf clean all

COPY keycloak/start-keycloak.sh /opt/keycloak/start-keycloak.sh
RUN chmod +x /opt/keycloak/start-keycloak.sh

USER 1000

ENTRYPOINT ["/opt/keycloak/start-keycloak.sh"]
