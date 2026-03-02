import axios, { type AxiosRequestConfig } from "axios";
import type {
    BaseRecord,
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

type ResourceName = "users" | "orders" | "roles" | "groups" | "clients";

type ListRecord = BaseRecord & Record<string, unknown>;

const endpointMap: Record<Exclude<ResourceName, "groups" | "clients">, string> = {
    users: "/api/users",
    orders: "/api/orders",
    roles: "/api/todos/debugroles",
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

const authHeaders = async (): Promise<AxiosRequestConfig["headers"]> => {
    const token = await getAccessToken();
    if (!token) return {};

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

const fetchList = async (apiUrl: string, resource: string): Promise<ListRecord[]> => {
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
        headers: await authHeaders(),
    });

    return normalizeList(resource, response.data);
};

export const keycloakDataProvider = (apiUrl: string): DataProvider => ({
    getApiUrl: () => apiUrl,
    getList: async <TData extends BaseRecord = BaseRecord>(
        params: GetListParams,
    ): Promise<GetListResponse<TData>> => {
        const normalized = (await fetchList(apiUrl, params.resource)) as TData[];
        return {
            data: normalized,
            total: normalized.length,
        };
    },
    getOne: async <TData extends BaseRecord = BaseRecord>(
        params: GetOneParams,
    ): Promise<GetOneResponse<TData>> => {
        const { resource, id } = params;
        const list = (await fetchList(apiUrl, resource)) as TData[];

        const found = list.find((item) => String(item.id) === String(id));
        if (!found) {
            throw new Error(`Record not found for resource '${resource}' and id '${id}'`);
        }

        return { data: found };
    },
    create: async <TData extends BaseRecord = BaseRecord, TVariables = object>(
        params: CreateParams<TVariables>,
    ): Promise<CreateResponse<TData>> => {
        const { resource, variables } = params;
        const endpoint = endpointMap[resource as keyof typeof endpointMap];

        if (!endpoint || resource === "roles") {
            throw new Error(`Create is not implemented for resource '${resource}'`);
        }

        const response = await axios.post(`${apiUrl}${endpoint}`, variables, {
            headers: await authHeaders(),
        });

        const data = toListRecord(response.data, 0) as TData;
        return { data };
    },
    update: async <TData extends BaseRecord = BaseRecord, TVariables = object>(
        params: UpdateParams<TVariables>,
    ): Promise<UpdateResponse<TData>> => {
        const { resource, id, variables } = params;
        const endpoint = endpointMap[resource as keyof typeof endpointMap];

        if (!endpoint || resource === "roles") {
            throw new Error(`Update is not implemented for resource '${resource}'`);
        }

        const response = await axios.put(`${apiUrl}${endpoint}/${id}`, variables, {
            headers: await authHeaders(),
        });

        const data = toListRecord(response.data, 0) as TData;
        return { data };
    },
    deleteOne: async <TData extends BaseRecord = BaseRecord, TVariables = object>(
        params: DeleteOneParams<TVariables>,
    ): Promise<DeleteOneResponse<TData>> => {
        const { resource, id } = params;
        const endpoint = endpointMap[resource as keyof typeof endpointMap];

        if (!endpoint || resource === "roles") {
            throw new Error(`Delete is not implemented for resource '${resource}'`);
        }

        await axios.delete(`${apiUrl}${endpoint}/${id}`, {
            headers: await authHeaders(),
        });

        return { data: { id } as TData };
    },
});
