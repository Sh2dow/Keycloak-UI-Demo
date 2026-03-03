import { useMemo, useState } from "react";
import { useCreate, useDelete, useInvalidate, useList, useUpdate } from "@refinedev/core";
import {
    Badge,
    Button,
    Card,
    Group,
    Loader,
    Modal,
    Stack,
    Table,
    Text,
    TextInput,
    Title,
} from "@mantine/core";
import { useNavigate } from "react-router-dom";

type OrderItem = {
    id: string;
    orderType: string;
    totalAmount: number;
    status: string;
    createdAtUtc: string;
    downloadUrl?: string | null;
    shippingAddress?: string | null;
    trackingNumber?: string | null;
};

type AppUser = {
    id: string;
    subject: string;
    username: string;
    email?: string | null;
    createdAtUtc: string;
    orders: OrderItem[];
};

export function UsersPage() {
    const navigate = useNavigate();
    const listQuery = useList<AppUser>({
        resource: "users",
    });
    const invalidate = useInvalidate();
    const { mutateAsync: createUser, isLoading: isCreating } = useCreate();
    const { mutateAsync: updateUser, isLoading: isUpdating } = useUpdate();
    const { mutateAsync: deleteUser, isLoading: isDeleting } = useDelete();

    const [subject, setSubject] = useState("");
    const [username, setUsername] = useState("");
    const [email, setEmail] = useState("");
    const [editing, setEditing] = useState<AppUser | null>(null);
    const [editUsername, setEditUsername] = useState("");
    const [editEmail, setEditEmail] = useState("");

    const sortedUsers = useMemo(() => {
        const users: AppUser[] = listQuery.data?.data ?? [];
        return [...users].sort((a, b) => a.username.localeCompare(b.username));
    }, [listQuery.data?.data]);

    const refresh = async () => {
        await invalidate({
            resource: "users",
            invalidates: ["list"],
        });
    };

    const onCreate = async () => {
        if (!subject.trim() || !username.trim()) return;

        await createUser({
            resource: "users",
            values: {
                subject: subject.trim(),
                username: username.trim(),
                email: email.trim() || null,
            },
        });

        setSubject("");
        setUsername("");
        setEmail("");
        await refresh();
    };

    const onOpenEdit = (user: AppUser) => {
        setEditing(user);
        setEditUsername(user.username);
        setEditEmail(user.email ?? "");
    };

    const onSaveEdit = async () => {
        if (!editing || !editUsername.trim()) return;

        await updateUser({
            resource: "users",
            id: editing.id,
            values: {
                username: editUsername.trim(),
                email: editEmail.trim() || null,
            },
        });

        setEditing(null);
        await refresh();
    };

    const onDelete = async (id: string) => {
        await deleteUser({
            resource: "users",
            id,
        });
        await refresh();
    };

    const exploreOrders = (id: string) => {
        navigate(`/orders?asUserId=${encodeURIComponent(id)}`);
    };

    const exploreTasks = (id: string) => {
        navigate(`/tasks?asUserId=${encodeURIComponent(id)}`);
    };

    if (listQuery.isLoading) {
        return <Loader />;
    }

    return (
        <Stack>
            <Title order={2}>Users</Title>
            <Card withBorder radius="md" padding="md">
                <Stack>
                    <Text fw={600}>Create User</Text>
                    <Group grow>
                        <TextInput
                            label="Subject"
                            placeholder="keycloak-sub"
                            value={subject}
                            onChange={(event) => setSubject(event.currentTarget.value)}
                        />
                        <TextInput
                            label="Username"
                            placeholder="username"
                            value={username}
                            onChange={(event) => setUsername(event.currentTarget.value)}
                        />
                        <TextInput
                            label="Email"
                            placeholder="user@example.com"
                            value={email}
                            onChange={(event) => setEmail(event.currentTarget.value)}
                        />
                    </Group>
                    <Group justify="flex-end">
                        <Button onClick={onCreate} loading={isCreating}>
                            Create
                        </Button>
                    </Group>
                </Stack>
            </Card>
            <Table striped withTableBorder withColumnBorders>
                <Table.Thead>
                    <Table.Tr>
                        <Table.Th>Username</Table.Th>
                        <Table.Th>Subject</Table.Th>
                        <Table.Th>Email</Table.Th>
                        <Table.Th>Orders</Table.Th>
                        <Table.Th>Actions</Table.Th>
                    </Table.Tr>
                </Table.Thead>
                <Table.Tbody>
                    {sortedUsers.map((user) => (
                        <Table.Tr key={user.id}>
                            <Table.Td>{user.username}</Table.Td>
                            <Table.Td>{user.subject}</Table.Td>
                            <Table.Td>{user.email ?? "-"}</Table.Td>
                            <Table.Td>{user.orders.length}</Table.Td>
                            <Table.Td>
                                <Group gap="xs">
                                    <Button size="xs" variant="light" onClick={() => onOpenEdit(user)}>
                                        Edit
                                    </Button>
                                    <Button size="xs" variant="light" color="teal" onClick={() => exploreOrders(user.id)}>
                                        Explore Orders
                                    </Button>
                                    <Button size="xs" variant="light" color="cyan" onClick={() => exploreTasks(user.id)}>
                                        Explore Tasks
                                    </Button>
                                    <Button
                                        size="xs"
                                        color="red"
                                        variant="light"
                                        loading={isDeleting}
                                        onClick={() => onDelete(user.id)}
                                    >
                                        Delete
                                    </Button>
                                </Group>
                            </Table.Td>
                        </Table.Tr>
                    ))}
                </Table.Tbody>
            </Table>

            {sortedUsers.map((user) => (
                <Card key={`orders-${user.id}`} withBorder radius="md" padding="md">
                    <Group justify="space-between" mb="sm">
                        <Text fw={700}>{user.username}</Text>
                        <Badge>{user.orders.length} orders</Badge>
                    </Group>
                    {user.orders.length === 0 ? (
                        <Text c="dimmed">No orders</Text>
                    ) : (
                        <Stack gap="xs">
                            {user.orders.map((order) => (
                                <Text key={order.id} size="sm">
                                    {order.orderType.toUpperCase()} | ${order.totalAmount.toFixed(2)} | {order.status}
                                </Text>
                            ))}
                        </Stack>
                    )}
                </Card>
            ))}

            <Modal opened={!!editing} onClose={() => setEditing(null)} title="Edit User">
                <Stack>
                    <TextInput
                        label="Username"
                        value={editUsername}
                        onChange={(event) => setEditUsername(event.currentTarget.value)}
                    />
                    <TextInput
                        label="Email"
                        value={editEmail}
                        onChange={(event) => setEditEmail(event.currentTarget.value)}
                    />
                    <Group justify="flex-end">
                        <Button variant="default" onClick={() => setEditing(null)}>
                            Cancel
                        </Button>
                        <Button onClick={onSaveEdit} loading={isUpdating}>
                            Save
                        </Button>
                    </Group>
                </Stack>
            </Modal>
        </Stack>
    );
}
