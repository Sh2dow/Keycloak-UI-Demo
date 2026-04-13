import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig(({ mode }) => {
    const env = loadEnv(mode, process.cwd(), "");
    const apiProxyTarget = env.VITE_API_PROXY_TARGET || "http://localhost:5000";
    const ordersProxyTarget = env.VITE_ORDERS_PROXY_TARGET || "http://localhost:5003";
    const tasksProxyTarget = env.VITE_TASKS_PROXY_TARGET || "http://localhost:5002";

    return {
        plugins: [react()],
        server: {
            port: parseInt(env.VITE_API_URL) || 5173,
            proxy: {
                "/api/users": apiProxyTarget,
                "/api/orders": ordersProxyTarget,
                "/api/tasks": tasksProxyTarget,
                "/api/roles": apiProxyTarget,
            },
        },
    };
});
