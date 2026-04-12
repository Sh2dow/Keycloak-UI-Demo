cd d:\Tools\keycloak-26.5.4\bin\
.\kc.bat start-dev ^
  --db postgres ^
  --db-url jdbc:postgresql://localhost:5432/keycloak_demo_auth ^
  --db-username keycloak ^
  --db-password 123