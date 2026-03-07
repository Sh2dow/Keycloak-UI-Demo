import { useState } from "react";
import axios from "axios";
import { useCreate, useDelete, useInvalidate, useList, useUpdate } from "@refinedev/core";
import {
    Badge,
    Button,
    Card,
    Group,
    Loader,
    Select,
    Stack,
    Table,
    Text,
    TextInput,
    Textarea,
    Title,
} from "@mantine/core";
import { useSearchParams } from "react-router-dom";
import { getAccessToken } from "../../providers/keycloakAuthProvider";

const API_URL = import.meta.env.VITE_API_URL ?? "";

type TaskComment = {
    id: string;
    authorId: string;
    authorUsername: string;
    content: string;
    createdAtUtc: string;
};

type TaskItem = {
    id: string;
    userId: string;
    title: string;
    description?: string | null;
    status: "todo" | "in-progress" | "done";
    priority: "low" | "medium" | "high";
    createdAtUtc: string;
    updatedAtUtc?: string | null;
    comments: TaskComment[];
};

export function TasksPage() {
    const [searchParams] = useSearchParams();
    const asUserId = searchParams.get("asUserId") ?? undefined;
    const listQuery = useList<TaskItem>({
        resource: "tasks",
        meta: asUserId ? { asUserId } : undefined,
    });
    const invalidate = useInvalidate();
    const { mutateAsync: createTask, isLoading: isCreating } = useCreate();
    const { mutateAsync: updateTask, isLoading: isUpdating } = useUpdate();
    const { mutateAsync: deleteTask, isLoading: isDeleting } = useDelete();

    const [title, setTitle] = useState("");
    const [description, setDescription] = useState("");
    const [status, setStatus] = useState<"todo" | "in-progress" | "done">("todo");
    const [priority, setPriority] = useState<"low" | "medium" | "high">("medium");
    const [commentDrafts, setCommentDrafts] = useState<Record<string, string>>({});

    if (listQuery.isLoading) return <Loader />;
    const tasks = listQuery.data?.data ?? [];

    const refresh = async () => {
        await invalidate({
            resource: "tasks",
            invalidates: ["list"],
        });
    };

    const onCreate = async () => {
        if (!title.trim()) return;

        await createTask({
            resource: "tasks",
            meta: asUserId ? { asUserId } : undefined,
            values: {
                title: title.trim(),
                description: description.trim() || null,
                status,
                priority,
            },
        });

        setTitle("");
        setDescription("");
        setStatus("todo");
        setPriority("medium");
        await refresh();
    };

    const onCycleStatus = async (task: TaskItem) => {
        const nextStatus =
            task.status === "todo" ? "in-progress" : task.status === "in-progress" ? "done" : "todo";

        await updateTask({
            resource: "tasks",
            id: task.id,
            meta: asUserId ? { asUserId } : undefined,
            values: {
                title: task.title,
                description: task.description ?? null,
                status: nextStatus,
                priority: task.priority,
            },
        });

        await refresh();
    };

    const onDelete = async (id: string) => {
        await deleteTask({
            resource: "tasks",
            id,
            meta: asUserId ? { asUserId } : undefined,
        });
        await refresh();
    };

    const onAddComment = async (taskId: string) => {
        const content = commentDrafts[taskId]?.trim();
        if (!content) return;

        const token = await getAccessToken();
        await axios.post(
            `${API_URL}/api/tasks/${taskId}/comments`,
            { content },
            {
                params: asUserId ? { asUserId } : undefined,
                headers: token ? { Authorization: `Bearer ${token}` } : {},
            },
        );

        setCommentDrafts((current) => ({ ...current, [taskId]: "" }));
        await refresh();
    };

    return (
        <Stack>
            <Title order={2}>Task Tracker</Title>
            {asUserId && (
                <Text c="dimmed" size="sm">
                    Explore mode: acting on user {asUserId}
                </Text>
            )}

            <Card withBorder radius="md" p="md">
                <Stack>
                    <Text fw={600}>Create Task</Text>
                    <Group grow align="flex-end">
                        <TextInput
                            label="Title"
                            placeholder="Prepare release notes"
                            value={title}
                            onChange={(event) => setTitle(event.currentTarget.value)}
                        />
                        <Select
                            label="Status"
                            value={status}
                            data={[
                                { label: "Todo", value: "todo" },
                                { label: "In Progress", value: "in-progress" },
                                { label: "Done", value: "done" },
                            ]}
                            onChange={(value) => setStatus((value as TaskItem["status"]) ?? "todo")}
                        />
                        <Select
                            label="Priority"
                            value={priority}
                            data={[
                                { label: "Low", value: "low" },
                                { label: "Medium", value: "medium" },
                                { label: "High", value: "high" },
                            ]}
                            onChange={(value) => setPriority((value as TaskItem["priority"]) ?? "medium")}
                        />
                    </Group>
                    <Textarea
                        label="Description"
                        placeholder="Optional details..."
                        value={description}
                        onChange={(event) => setDescription(event.currentTarget.value)}
                        minRows={2}
                    />
                    <Group justify="flex-end">
                        <Button onClick={onCreate} loading={isCreating}>
                            Add Task
                        </Button>
                    </Group>
                </Stack>
            </Card>

            <Table striped withTableBorder withColumnBorders>
                <Table.Thead>
                    <Table.Tr>
                        <Table.Th>Title</Table.Th>
                        <Table.Th>Status</Table.Th>
                        <Table.Th>Priority</Table.Th>
                        <Table.Th>Comments</Table.Th>
                        <Table.Th>Actions</Table.Th>
                    </Table.Tr>
                </Table.Thead>
                <Table.Tbody>
                    {tasks.map((task) => (
                        <Table.Tr key={task.id}>
                            <Table.Td>
                                <Text fw={600}>{task.title}</Text>
                                <Text size="sm" c="dimmed">
                                    {task.description || "-"}
                                </Text>
                            </Table.Td>
                            <Table.Td>
                                <Badge variant="light">{task.status}</Badge>
                            </Table.Td>
                            <Table.Td>
                                <Badge variant="outline">{task.priority}</Badge>
                            </Table.Td>
                            <Table.Td>{task.comments.length}</Table.Td>
                            <Table.Td>
                                <Group gap="xs">
                                    <Button size="xs" variant="light" loading={isUpdating} onClick={() => onCycleStatus(task)}>
                                        Cycle Status
                                    </Button>
                                    <Button
                                        size="xs"
                                        color="red"
                                        variant="light"
                                        loading={isDeleting}
                                        onClick={() => onDelete(task.id)}
                                    >
                                        Delete
                                    </Button>
                                </Group>
                            </Table.Td>
                        </Table.Tr>
                    ))}
                </Table.Tbody>
            </Table>

            {tasks.map((task) => (
                <Card key={`comments-${task.id}`} withBorder radius="md" p="md">
                    <Stack>
                        <Group justify="space-between">
                            <Text fw={700}>{task.title}</Text>
                            <Badge>{task.comments.length} comments</Badge>
                        </Group>

                        {task.comments.length === 0 ? (
                            <Text c="dimmed" size="sm">
                                No comments yet.
                            </Text>
                        ) : (
                            <Stack gap={6}>
                                {task.comments.map((comment) => (
                                    <Text key={comment.id} size="sm">
                                        <strong>{comment.authorUsername}</strong>: {comment.content}
                                    </Text>
                                ))}
                            </Stack>
                        )}

                        <Group align="flex-end" grow>
                            <TextInput
                                label="New Comment"
                                placeholder="Add a comment..."
                                value={commentDrafts[task.id] ?? ""}
                                onChange={(event) =>
                                    setCommentDrafts((current) => ({
                                        ...current,
                                        [task.id]: event.currentTarget.value,
                                    }))
                                }
                            />
                            <Button onClick={() => onAddComment(task.id)}>Post</Button>
                        </Group>
                    </Stack>
                </Card>
            ))}
        </Stack>
    );
}
