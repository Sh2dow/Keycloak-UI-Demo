import { useAuth } from "react-oidc-context";
import { api } from "./api";
import { useState } from "react";

type TodoItem = {
    id: string;
    title: string;
    isDone: boolean;
    userSub: string;
    createdAtUtc: string;
};

export default function App() {
    const auth = useAuth();
    const [title, setTitle] = useState("");
    const [todos, setTodos] = useState<TodoItem[]>([]);

    const getAccessToken = async () => {
        const token = auth.user?.access_token;
        if (token) return token;
        await auth.signinRedirect();
        return null;
    };

    const load = async () => {
        const token = await getAccessToken();
        if (!token) return;
        const res = await api(token).get<TodoItem[]>("/api/todos");
        setTodos(res.data);
    };

    const create = async () => {
        if (!title.trim()) return;
        const token = await getAccessToken();
        if (!token) return;
        await api(token).post("/api/todos", { title });
        setTitle("");
        await load();
    };

    if (auth.isLoading) return <div>Loading auth…</div>;

    if (auth.error) return <div>Auth error: {String(auth.error)}</div>;

    if (!auth.isAuthenticated) {
        return (
            <div style={{ padding: 24 }}>
                <h2>Keycloak + React + .NET + EF Core</h2>
                <button onClick={() => auth.signinRedirect()}>Login</button>
            </div>
        );
    }

    return (
        <div style={{ padding: 24 }}>
            <h2>Logged in</h2>

            <div style={{ display: "flex", gap: 8, marginBottom: 12 }}>
                <button onClick={load}>Load Todos</button>
                <button onClick={() => auth.signoutRedirect()}>Logout</button>
            </div>

            <div style={{ display: "flex", gap: 8, marginBottom: 12 }}>
                <input
                    value={title}
                    onChange={(e) => setTitle(e.target.value)}
                    placeholder="New todo title"
                />
                <button onClick={create}>Create</button>
            </div>

            <pre style={{ background: "#111", color: "#ddd", padding: 12 }}>
        {JSON.stringify(todos, null, 2)}
      </pre>

            <div style={{ marginTop: 12 }}>
                <button
                    onClick={async () => {
                        const token = await getAccessToken();
                        if (!token) return;
                        const res = await api(token).get("/api/todos/admin");
                        alert(res.data);
                    }}
                >
                    Call admin endpoint
                </button>
                <div style={{ fontSize: 12, opacity: 0.8 }}>
                    (Works only if your Keycloak user has realm role <code>admin</code>)
                </div>
            </div>
        </div>
    );
}
