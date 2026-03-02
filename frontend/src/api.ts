import axios from "axios";

export function api(accessToken?: string | null) {
    const instance = axios.create({
        baseURL: "http://localhost:5274", // Vite proxy handles /api -> backend
    });

    instance.interceptors.request.use((config) => {
        if (accessToken) {
            config.headers = config.headers ?? {};
            config.headers.Authorization = `Bearer ${accessToken}`;
        }
        return config;
    });

    return instance;
}