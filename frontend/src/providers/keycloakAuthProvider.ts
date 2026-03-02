import type { AuthBindings } from "@refinedev/core";
import { UserManager, type UserManagerSettings } from "oidc-client-ts";

const oidcSettings: UserManagerSettings = {
    authority: "http://localhost:8080/realms/myrealm",
    client_id: "react-client",
    redirect_uri: "http://localhost:5173",
    post_logout_redirect_uri: "http://localhost:5173/login",
    response_type: "code",
    scope: "openid profile email",
    automaticSilentRenew: true,
};

export const keycloakUserManager = new UserManager(oidcSettings);

export const keycloakAuthProvider: AuthBindings = {
    login: async () => {
        await keycloakUserManager.signinRedirect();
        return { success: true };
    },
    logout: async () => {
        await keycloakUserManager.signoutRedirect();
        return { success: true, redirectTo: "/login" };
    },
    check: async () => {
        const callbackUrl = new URL(window.location.href);
        const hasAuthParams = callbackUrl.searchParams.has("code") && callbackUrl.searchParams.has("state");

        if (hasAuthParams) {
            await keycloakUserManager.signinRedirectCallback();
            window.history.replaceState({}, document.title, "/");
        }

        const user = await keycloakUserManager.getUser();

        if (user && !user.expired) {
            return { authenticated: true };
        }

        return {
            authenticated: false,
            logout: true,
            redirectTo: "/login",
        };
    },
    getPermissions: async () => {
        const user = await keycloakUserManager.getUser();
        const realmAccess = user?.profile?.realm_access as { roles?: string[] } | undefined;
        return realmAccess?.roles ?? [];
    },
    getIdentity: async () => {
        const user = await keycloakUserManager.getUser();
        if (!user) return null;

        return {
            id: String(user.profile.sub ?? ""),
            name: String(user.profile.preferred_username ?? user.profile.name ?? "Unknown"),
        };
    },
    onError: async () => {
        return {};
    },
};

export async function getAccessToken(): Promise<string | null> {
    const user = await keycloakUserManager.getUser();
    if (!user || user.expired || !user.access_token) {
        return null;
    }

    return user.access_token;
}
