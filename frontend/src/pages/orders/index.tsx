import { useState } from "react";
import { useCreate, useDelete, useInvalidate, useList, useUpdate } from "@refinedev/core";
import {
    Button,
    Group,
    Loader,
    Modal,
    Select,
    Stack,
    Table,
    TextInput,
    Title,
} from "@mantine/core";

type OrderItem = {
    id: string;
    orderType: "digital" | "physical";
    totalAmount: number;
    status: string;
    createdAtUtc: string;
    downloadUrl?: string | null;
    shippingAddress?: string | null;
    trackingNumber?: string | null;
};

export function OrdersPage() {
    const listQuery = useList<OrderItem>({ resource: "orders" });
    const invalidate = useInvalidate();
    const { mutateAsync: createOrder, isLoading: isCreating } = useCreate();
    const { mutateAsync: updateOrder, isLoading: isUpdating } = useUpdate();
    const { mutateAsync: deleteOrder, isLoading: isDeleting } = useDelete();

    const [orderType, setOrderType] = useState<"digital" | "physical">("digital");
    const [amount, setAmount] = useState("49.99");
    const [downloadUrl, setDownloadUrl] = useState("https://example.com/download/item");
    const [shippingAddress, setShippingAddress] = useState("221B Baker Street, London");
    const [trackingNumber, setTrackingNumber] = useState("");

    const [editing, setEditing] = useState<OrderItem | null>(null);
    const [editAmount, setEditAmount] = useState("0");
    const [editStatus, setEditStatus] = useState("Created");
    const [editDownloadUrl, setEditDownloadUrl] = useState("");
    const [editShippingAddress, setEditShippingAddress] = useState("");
    const [editTrackingNumber, setEditTrackingNumber] = useState("");

    if (listQuery.isLoading) return <Loader />;
    const orders = listQuery.data?.data ?? [];

    const refresh = async () => {
        await invalidate({
            resource: "orders",
            invalidates: ["list"],
        });
    };

    const onCreate = async () => {
        const totalAmount = Number(amount);
        if (!Number.isFinite(totalAmount) || totalAmount <= 0) return;

        await createOrder({
            resource: "orders",
            values: {
                orderType,
                totalAmount,
                downloadUrl: orderType === "digital" ? downloadUrl.trim() : null,
                shippingAddress: orderType === "physical" ? shippingAddress.trim() : null,
                trackingNumber: orderType === "physical" ? trackingNumber.trim() || null : null,
            },
        });

        await refresh();
    };

    const openEdit = (order: OrderItem) => {
        setEditing(order);
        setEditAmount(String(order.totalAmount));
        setEditStatus(order.status);
        setEditDownloadUrl(order.downloadUrl ?? "");
        setEditShippingAddress(order.shippingAddress ?? "");
        setEditTrackingNumber(order.trackingNumber ?? "");
    };

    const onSaveEdit = async () => {
        if (!editing) return;
        const totalAmount = Number(editAmount);
        if (!Number.isFinite(totalAmount) || totalAmount <= 0 || !editStatus.trim()) return;

        await updateOrder({
            resource: "orders",
            id: editing.id,
            values: {
                totalAmount,
                status: editStatus.trim(),
                downloadUrl: editing.orderType === "digital" ? editDownloadUrl.trim() : null,
                shippingAddress: editing.orderType === "physical" ? editShippingAddress.trim() : null,
                trackingNumber: editing.orderType === "physical" ? editTrackingNumber.trim() || null : null,
            },
        });

        setEditing(null);
        await refresh();
    };

    const onDelete = async (id: string) => {
        await deleteOrder({
            resource: "orders",
            id,
        });
        await refresh();
    };

    return (
        <Stack>
            <Title order={2}>Orders</Title>

            <Group align="flex-end" grow>
                <Select
                    label="Order Type"
                    data={[
                        { label: "Digital", value: "digital" },
                        { label: "Physical", value: "physical" },
                    ]}
                    value={orderType}
                    onChange={(value) => setOrderType((value as "digital" | "physical") ?? "digital")}
                />
                <TextInput
                    label="Amount"
                    value={amount}
                    onChange={(event) => setAmount(event.currentTarget.value)}
                />
                {orderType === "digital" ? (
                    <TextInput
                        label="Download URL"
                        value={downloadUrl}
                        onChange={(event) => setDownloadUrl(event.currentTarget.value)}
                    />
                ) : (
                    <TextInput
                        label="Shipping Address"
                        value={shippingAddress}
                        onChange={(event) => setShippingAddress(event.currentTarget.value)}
                    />
                )}
                {orderType === "physical" && (
                    <TextInput
                        label="Tracking Number"
                        value={trackingNumber}
                        onChange={(event) => setTrackingNumber(event.currentTarget.value)}
                    />
                )}
                <Button onClick={onCreate} loading={isCreating}>
                    Create
                </Button>
            </Group>

            <Table striped withTableBorder withColumnBorders>
                <Table.Thead>
                    <Table.Tr>
                        <Table.Th>Type</Table.Th>
                        <Table.Th>Amount</Table.Th>
                        <Table.Th>Status</Table.Th>
                        <Table.Th>Details</Table.Th>
                        <Table.Th>Created</Table.Th>
                        <Table.Th>Actions</Table.Th>
                    </Table.Tr>
                </Table.Thead>
                <Table.Tbody>
                    {orders.map((order) => (
                        <Table.Tr key={order.id}>
                            <Table.Td>{order.orderType}</Table.Td>
                            <Table.Td>${order.totalAmount.toFixed(2)}</Table.Td>
                            <Table.Td>{order.status}</Table.Td>
                            <Table.Td>
                                {order.orderType === "digital"
                                    ? order.downloadUrl ?? "-"
                                    : `${order.shippingAddress ?? "-"} (${order.trackingNumber ?? "-"})`}
                            </Table.Td>
                            <Table.Td>{new Date(order.createdAtUtc).toLocaleString()}</Table.Td>
                            <Table.Td>
                                <Group gap="xs">
                                    <Button size="xs" variant="light" onClick={() => openEdit(order)}>
                                        Edit
                                    </Button>
                                    <Button
                                        size="xs"
                                        color="red"
                                        variant="light"
                                        loading={isDeleting}
                                        onClick={() => onDelete(order.id)}
                                    >
                                        Delete
                                    </Button>
                                </Group>
                            </Table.Td>
                        </Table.Tr>
                    ))}
                </Table.Tbody>
            </Table>

            <Modal opened={!!editing} onClose={() => setEditing(null)} title="Edit Order">
                <Stack>
                    <TextInput
                        label="Amount"
                        value={editAmount}
                        onChange={(event) => setEditAmount(event.currentTarget.value)}
                    />
                    <TextInput
                        label="Status"
                        value={editStatus}
                        onChange={(event) => setEditStatus(event.currentTarget.value)}
                    />
                    {editing?.orderType === "digital" ? (
                        <TextInput
                            label="Download URL"
                            value={editDownloadUrl}
                            onChange={(event) => setEditDownloadUrl(event.currentTarget.value)}
                        />
                    ) : (
                        <>
                            <TextInput
                                label="Shipping Address"
                                value={editShippingAddress}
                                onChange={(event) => setEditShippingAddress(event.currentTarget.value)}
                            />
                            <TextInput
                                label="Tracking Number"
                                value={editTrackingNumber}
                                onChange={(event) => setEditTrackingNumber(event.currentTarget.value)}
                            />
                        </>
                    )}
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

