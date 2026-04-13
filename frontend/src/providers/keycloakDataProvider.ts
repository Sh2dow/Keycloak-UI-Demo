import axios, { type AxiosRequestConfig } from "axios";
import type {
    BaseRecord,
    CustomParams,
    CustomResponse,
    CreateParams,
    CreateResponse,
    DataProvider,
    DeleteOneParams,
    DeleteOneResponse,
    GetListParams,
    GetListResponse,
    GetOneParams,
    GetOneResponse,
    UpdateParams,
    UpdateResponse,
} from "@refinedev/core";
import { getAccessToken, keycloakUserManager } from "./keycloakAuthProvider";

type ResourceName = "users" | "orders" | "tasks" | "roles" | "groups" | "clients";

type ListRecord = BaseRecord & Record<string, unknown>;

const endpointMap: Record<Exclude<ResourceName, "groups" | "clients">, string> = {
    users: "/api/users",
    orders: "/api/orders",
    tasks: "/api/tasks",
    roles: "/api/tasks/debugroles",
};

const toListRecord = (value: unknown, index: number): ListRecord => {
    if (value && typeof value === "object") {
        const objectValue = value as Record<string, unknown>;
        return {
            id: String(objectValue.id ?? index),
            ...objectValue,
        };
    }

    return {
        id: String(index),
        value,
    };
};

const getAsUserId = (resource: string, meta: unknown): string | undefined => {
    if (resource !== "orders" && resource !== "tasks") {
        return undefined;
    }

    if (!meta || typeof meta !== "object") {
        return undefined;
    }

    const asUserId = (meta as { asUserId?: unknown }).asUserId;
    if (typeof asUserId !== "string" || asUserId.trim().length === 0) {
        return undefined;
    }

    return asUserId.trim();
};

const getAsUserIdFromMeta = (meta: unknown): string | undefined => {
    if (!meta || typeof meta !== "object") {
        return undefined;
    }

    const asUserId = (meta as { asUserId?: unknown }).asUserId;
    if (typeof asUserId !== "string" || asUserId.trim().length === 0) {
        return undefined;
    }

    return asUserId.trim();
};

const withAsUserId = (endpoint: string, asUserId?: string): string => {
    if (!asUserId) return endpoint;
    const separator = endpoint.includes("?") ? "&" : "?";
    return `${endpoint}${separator}asUserId=${encodeURIComponent(asUserId)}`;
};

const normalizeList = (resource: string, payload: unknown): ListRecord[] => {
    if (!Array.isArray(payload)) {
        return [];
    }

    if (resource === "roles") {
        return payload.map((item, index) => {
            const roleName = String(item);
            return {
                id: roleName || String(index),
                name: roleName,
            };
        });
    }

    return payload.map((item, index) => toListRecord(item, index));
};

const authHeaders = async (asUserId?: string): Promise<AxiosRequestConfig["headers"]> => {
    const token = await getAccessToken(false, asUserId);
    if (!token) {
        // If impersonating and token is missing/expired, force re-login
        if (asUserId) {
            try {
                await keycloakUserManager.signinRedirect();
            } catch {
                // Ignore redirect errors
            }
            throw new Error("Authentication expired. Please log in again to use impersonation.");
        }
        return {};
    }

    return {
        Authorization: `Bearer ${token}`,
    };
};

const readGroupsFromToken = async (): Promise<ListRecord[]> => {
    const user = await keycloakUserManager.getUser();
    const groups = user?.profile?.groups;

    if (!Array.isArray(groups)) return [];

    return groups.map((group, index) => ({
        id: String(group ?? index),
        name: String(group ?? ""),
    }));
};

const readClientsFromToken = async (): Promise<ListRecord[]> => {
    const user = await keycloakUserManager.getUser();
    const azp = user?.profile?.azp;
    const aud = user?.profile?.aud;

    const clients = new Set<string>();

    if (typeof azp === "string" && azp.length > 0) {
        clients.add(azp);
    }

    if (Array.isArray(aud)) {
        for (const item of aud) {
            if (typeof item === "string" && item.length > 0) {
                clients.add(item);
            }
        }
    } else if (typeof aud === "string" && aud.length > 0) {
        clients.add(aud);
    }

    return Array.from(clients).map((client, index) => ({
        id: `${client}-${index}`,
        clientId: client,
        name: client,
    }));
};

const fetchList = async (apiUrl: string, resource: string, asUserId?: string): Promise<ListRecord[]> => {
    if (resource === "groups") {
        return readGroupsFromToken();
    }

    if (resource === "clients") {
        return readClientsFromToken();
    }

    const endpoint = endpointMap[resource as keyof typeof endpointMap];
    if (!endpoint) {
        return [];
    }

    const response = await axios.get(`${apiUrl}${endpoint}`, {
        params: asUserId ? { asUserId } : undefined,
        headers: await authHeaders(asUserId),
    });

    return normalizeList(resource, response.data);
};

export const keycloakDataProvider = (apiUrl: string): DataProvider => ({
    getApiUrl: () => apiUrl,
    getList: async <TData extends BaseRecord = BaseRecord>(
        params: GetListParams,
    ): Promise<GetListResponse<TData>> => {
        const asUserId = getAsUserId(params.resource, params.meta);
        const normalized = (await fetchList(apiUrl, params.resource, asUserId)) as TData[];
        return {
            data: normalized,
            total: normalized.length,
        };
    },
    getOne: async <TData extends BaseRecord = BaseRecord>(
        params: GetOneParams,
    ): Promise<GetOneResponse<TData>> => {
        const { resource, id } = params;
        const endpoint = endpointMap[resource as keyof typeof endpointMap];
        const asUserId = getAsUserId(resource, params.meta);

        if (resource === "groups" || resource === "clients") {
            const list = (await fetchList(apiUrl, resource, asUserId)) as TData[];
            const found = list.find((item) => String(item.id) === String(id));
            if (!found) {
                throw new Error(`Record not found for resource '${resource}' and id '${id}'`);
            }

            return { data: found };
        }

        if (!endpoint) {
            throw new Error(`GetOne is not implemented for resource '${resource}'`);
        }

        // Force token refresh when impersonating to ensure admin role is present
        if (asUserId) {
            try {
                await keycloakUserManager.signinSilent();
            } catch {
                try {
                    await keycloakUserManager.signinRedirect();
                } catch {
                    // Silent and redirect refresh failed, continue with current token
                }
            }
        }
        
        const response = await axios.get(`${apiUrl}${withAsUserId(`${endpoint}/${id}`, asUserId)}`, {
            headers: await authHeaders(asUserId),
        });

        return { data: toListRecord(response.data, 0) as TData };
    },
    create: async <TData extends BaseRecord = BaseRecord, TVariables = object>(
        params: CreateParams<TVariables>,
    ): Promise<CreateResponse<TData>> => {
        const { resource, variables } = params;
        const endpoint = endpointMap[resource as keyof typeof endpointMap];
        const asUserId = getAsUserId(resource, params.meta);

        if (!endpoint || resource === "roles") {
            throw new Error(`Create is not implemented for resource '${resource}'`);
        }

        // Force token refresh when impersonating to ensure admin role is present
        if (asUserId) {
            try {
                await keycloakUserManager.signinSilent();
            } catch {
                try {
                    await keycloakUserManager.signinRedirect();
                } catch {
                    // Silent and redirect refresh failed, continue with current token
                }
            }
        }
        
        const response = await axios.post(`${apiUrl}${withAsUserId(endpoint, asUserId)}`, variables, {
            headers: await authHeaders(asUserId),
        });

        const data = toListRecord(response.data, 0) as TData;
        return { data };
    },
    update: async <TData extends BaseRecord = BaseRecord, TVariables = object>(
        params: UpdateParams<TVariables>,
    ): Promise<UpdateResponse<TData>> => {
        const { resource, id, variables } = params;
        const endpoint = endpointMap[resource as keyof typeof endpointMap];
        const asUserId = getAsUserId(resource, params.meta);

        if (!endpoint || resource === "roles") {
            throw new Error(`Update is not implemented for resource '${resource}'`);
        }

        // Force token refresh when impersonating to ensure admin role is present
        if (asUserId) {
            try {
                await keycloakUserManager.signinSilent();
            } catch {
                try {
                    await keycloakUserManager.signinRedirect();
                } catch {
                    // Silent and redirect refresh failed, continue with current token
                }
            }
        }
        
        const response = await axios.put(`${apiUrl}${withAsUserId(`${endpoint}/${id}`, asUserId)}`, variables, {
            headers: await authHeaders(asUserId),
        });

        const data = toListRecord(response.data, 0) as TData;
        return { data };
    },
    deleteOne: async <TData extends BaseRecord = BaseRecord, TVariables = object>(
        params: DeleteOneParams<TVariables>,
    ): Promise<DeleteOneResponse<TData>> => {
        const { resource, id } = params;
        const endpoint = endpointMap[resource as keyof typeof endpointMap];
        const asUserId = getAsUserId(resource, params.meta);

        if (!endpoint || resource === "roles") {
            throw new Error(`Delete is not implemented for resource '${resource}'`);
        }

        // Force token refresh when impersonating to ensure admin role is present
        if (asUserId) {
            try {
                await keycloakUserManager.signinSilent();
            } catch {
                try {
                    await keycloakUserManager.signinRedirect();
                } catch {
                    // Silent and redirect refresh failed, continue with current token
                }
            }
        }
        
        await axios.delete(`${apiUrl}${withAsUserId(`${endpoint}/${id}`, asUserId)}`, {
            headers: await authHeaders(asUserId),
        });

        return { data: { id } as TData };
    },
    custom: async <TData extends BaseRecord = BaseRecord, TQuery = unknown, TPayload = unknown>(
        params: CustomParams<TQuery, TPayload>,
    ): Promise<CustomResponse<TData>> => {
        const asUserId = getAsUserIdFromMeta(params.meta);
        
        // Force token refresh when impersonating to ensure admin role is present
        if (asUserId) {
            try {
                await keycloakUserManager.signinSilent();
            } catch {
                try {
                    await keycloakUserManager.signinRedirect();
                } catch {
                    // Silent and redirect refresh failed, continue with current token
                }
            }
        }
        
        const headers = {
            ...(await authHeaders(asUserId)),
            ...(params.headers ?? {}),
        };

        const response = await axios({
            url: `${apiUrl}${withAsUserId(params.url, asUserId)}`,
            method: params.method,
            params: params.query,
            data: params.payload,
            headers,
        });

        return { data: response.data as TData };
    },
});
