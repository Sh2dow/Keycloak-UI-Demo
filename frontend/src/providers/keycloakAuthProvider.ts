import type { AuthBindings } from "@refinedev/core";
import { UserManager, type UserManagerSettings } from "oidc-client-ts";

const appOrigin = window.location.origin;
const oidcSettings: UserManagerSettings = {
    authority: import.meta.env.VITE_KEYCLOAK_AUTHORITY ?? "http://localhost:8080/realms/myrealm",
    client_id: "react-client",
    redirect_uri: appOrigin,
    post_logout_redirect_uri: `${appOrigin}/login`,
    response_type: "code",
    scope: "openid profile email",
    automaticSilentRenew: true,
    disablePKCE: true,
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

function isTokenExpired(token: string, clockSkewSeconds = 60): boolean {
    try {
        const payload = JSON.parse(atob(token.split(".")[1]));
        const exp = typeof payload.exp === "number" ? payload.exp : 0;
        return Date.now() >= (exp - clockSkewSeconds) * 1000;
    } catch {
        return true;
    }
}

export async function getAccessToken(forceRefresh = false, asUserId?: string): Promise<string | null> {
    let user = await keycloakUserManager.getUser();
    const tokenExpired = !!user?.access_token && isTokenExpired(user.access_token);
    
    // Refresh if forced, or if impersonating and token is expired/missing
    const needsRefresh = forceRefresh || (asUserId && (!user || user.expired || !user.access_token || tokenExpired));
    if (needsRefresh && user) {
        try {
            await keycloakUserManager.signinSilent();
            user = await keycloakUserManager.getUser();
        } catch (error) {
            // Silent refresh failed, try full redirect
            try {
                await keycloakUserManager.signinRedirect();
                user = await keycloakUserManager.getUser();
            } catch (redirectErr) {
                return null;
            }
        }
    }
    
    if (!user || user.expired || !user.access_token) {
        return null;
    }

    // Final safety check: if token is expired, redirect to login when impersonating
    if (asUserId && isTokenExpired(user.access_token)) {
        try {
            await keycloakUserManager.signinRedirect();
        } catch {
            // ignore
        }
        return null;
    }

    return user.access_token;
}

// Refresh the user token (useful for impersonation)
export async function refreshUserToken(): Promise<void> {
    try {
        await keycloakUserManager.signinSilent();
    } catch {
        // Silent refresh failed, try full redirect
        try {
            await keycloakUserManager.signinRedirect();
        } catch {
            // Redirect failed
        }
    }
}
