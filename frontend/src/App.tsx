import { useState } from "react";
import { Authenticated, Refine, useGetIdentity, useLogin, useLogout } from "@refinedev/core";
import routerProvider, {
    CatchAllNavigate,
    NavigateToResource,
    UnsavedChangesNotifier,
} from "@refinedev/react-router-v6";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
    AppShell,
    Box,
    Burger,
    Button,
    Flex,
    Group,
    MantineProvider,
    NavLink,
    Paper,
    Stack,
    Text,
    Title,
} from "@mantine/core";
import { Notifications } from "@mantine/notifications";
import { BrowserRouter, Link, Outlet, Route, Routes, useLocation } from "react-router-dom";
import { UsersPage } from "./pages/users";
import { OrdersPage } from "./pages/orders";
import { TasksPage } from "./pages/tasks";
import { RolesPage } from "./pages/roles";
import { GroupsPage } from "./pages/groups";
import { ClientsPage } from "./pages/clients";
import { keycloakAuthProvider } from "./providers/keycloakAuthProvider";
import { keycloakDataProvider } from "./providers/keycloakDataProvider";

const API_URL = import.meta.env.VITE_API_URL ?? "";
const queryClient = new QueryClient();

function LoginPage() {
    const { mutate: login, isLoading } = useLogin();

    return (
        <Flex align="center" justify="center" h="100vh" bg="gray.1">
            <Paper withBorder radius="md" p="xl" w={360}>
                <Stack>
                    <Title order={3}>Sign In</Title>
                    <Text c="dimmed" size="sm">
                        Authenticate with Keycloak to access Refine admin pages.
                    </Text>
                    <Button loading={isLoading} onClick={() => login({})}>
                        Login with Keycloak
                    </Button>
                </Stack>
            </Paper>
        </Flex>
    );
}

function ShellLayout() {
    const [opened, setOpened] = useState(true);
    const location = useLocation();
    const { data: identity } = useGetIdentity<{ id: string; name: string }>();
    const { mutate: logout } = useLogout();

    return (
        <AppShell
            header={{ height: 56 }}
            navbar={{ width: 260, breakpoint: "sm", collapsed: { mobile: !opened } }}
            padding="md"
        >
            <AppShell.Header>
                <Group h="100%" px="md" justify="space-between">
                    <Group>
                        <Burger opened={opened} onClick={() => setOpened((value) => !value)} hiddenFrom="sm" size="sm" />
                        <Text fw={700}>Keycloak Refine Console</Text>
                    </Group>
                    <Group>
                        <Text size="sm" c="dimmed">
                            {identity?.name ?? "Unknown"}
                        </Text>
                        <Button size="xs" variant="light" color="red" onClick={() => logout({})}>
                            Logout
                        </Button>
                    </Group>
                </Group>
            </AppShell.Header>
            <AppShell.Navbar p="md">
                <Stack gap="xs">
                    <NavLink component={Link} to="/users" label="Users" active={location.pathname.startsWith("/users")} />
                    <NavLink component={Link} to="/orders" label="Orders" active={location.pathname.startsWith("/orders")} />
                    <NavLink component={Link} to="/tasks" label="Tasks" active={location.pathname.startsWith("/tasks")} />
                    <NavLink component={Link} to="/roles" label="Roles" active={location.pathname.startsWith("/roles")} />
                    <NavLink component={Link} to="/groups" label="Groups" active={location.pathname.startsWith("/groups")} />
                    <NavLink component={Link} to="/clients" label="Clients" active={location.pathname.startsWith("/clients")} />
                </Stack>
            </AppShell.Navbar>
            <AppShell.Main>
                <Box maw={1200}>
                    <Outlet />
                </Box>
            </AppShell.Main>
        </AppShell>
    );
}

export default function App() {
    return (
        <MantineProvider defaultColorScheme="light">
            <Notifications />
            <BrowserRouter>
                <QueryClientProvider client={queryClient}>
                    <Refine
                        authProvider={keycloakAuthProvider}
                        dataProvider={keycloakDataProvider(API_URL)}
                        routerProvider={routerProvider}
                        resources={[
                            { name: "users", list: "/users" },
                            { name: "orders", list: "/orders" },
                            { name: "tasks", list: "/tasks" },
                            { name: "roles", list: "/roles" },
                            { name: "groups", list: "/groups" },
                            { name: "clients", list: "/clients" },
                        ]}
                        options={{
                            syncWithLocation: true,
                            warnWhenUnsavedChanges: true,
                        }}
                    >
                        <Routes>
                            <Route
                                element={
                                    <Authenticated key="protected-routes" fallback={<CatchAllNavigate to="/login" />}>
                                        <ShellLayout />
                                    </Authenticated>
                                }
                            >
                                <Route index element={<NavigateToResource resource="users" />} />
                                <Route path="/users" element={<UsersPage />} />
                                <Route path="/orders" element={<OrdersPage />} />
                                <Route path="/tasks" element={<TasksPage />} />
                                <Route path="/roles" element={<RolesPage />} />
                                <Route path="/groups" element={<GroupsPage />} />
                                <Route path="/clients" element={<ClientsPage />} />
                            </Route>
                            <Route
                                path="/login"
                                element={
                                    <Authenticated key="login-route" fallback={<LoginPage />}>
                                        <NavigateToResource resource="users" />
                                    </Authenticated>
                                }
                            />
                            <Route path="*" element={<CatchAllNavigate to="/users" />} />
                        </Routes>
                        <UnsavedChangesNotifier />
                    </Refine>
                </QueryClientProvider>
            </BrowserRouter>
        </MantineProvider>
    );
}
