export const oidcConfig = {
    authority: "http://localhost:8080/realms/myrealm",
    client_id: "react-client",
    redirect_uri: "http://localhost:5173",
    response_type: "code",
    scope: "openid profile email",
};