import { useEffect, useState } from "react";
import { useCreate, useDelete, useInvalidate, useList } from "@refinedev/core";
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
    Title,
} from "@mantine/core";
import { Link, useSearchParams } from "react-router-dom";
import { getStatusMeta, paymentPendingStatuses, type OrderItem } from "./shared";

export function OrdersPage() {
    const [searchParams] = useSearchParams();
    const asUserId = searchParams.get("asUserId") ?? undefined;
    const listQuery = useList<OrderItem>({
        resource: "orders",
        meta: asUserId ? { asUserId } : undefined,
    });
    const invalidate = useInvalidate();
    const { mutateAsync: createOrder, isLoading: isCreating } = useCreate();
    const { mutateAsync: deleteOrder, isLoading: isDeleting } = useDelete();

    const [orderType, setOrderType] = useState<"digital" | "physical">("digital");
    const [amount, setAmount] = useState("49.99");
    const [downloadUrl, setDownloadUrl] = useState("https://example.com/download/item");
    const [shippingAddress, setShippingAddress] = useState("221B Baker Street, London");
    const [trackingNumber, setTrackingNumber] = useState("");
    const orders = listQuery.data?.data ?? [];
    const pendingOrders = orders.filter((order) => paymentPendingStatuses.has(order.status));

    useEffect(() => {
        if (pendingOrders.length === 0) {
            return undefined;
        }

        const timer = window.setInterval(() => {
            void listQuery.refetch();
        }, 3000);

        return () => {
            window.clearInterval(timer);
        };
    }, [listQuery, pendingOrders.length]);

    if (listQuery.isLoading) return <Loader />;

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
            meta: asUserId ? { asUserId } : undefined,
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

    const onDelete = async (id: string) => {
        await deleteOrder({
            resource: "orders",
            id,
            meta: asUserId ? { asUserId } : undefined,
        });
        await refresh();
    };

    return (
        <Stack>
            <Group justify="space-between" align="flex-end">
                <div>
                    <Title order={2}>Orders</Title>
                    <Text c="dimmed" size="sm">
                        Orders now follow a payment and execution workflow instead of free-form status edits.
                    </Text>
                </div>
                {pendingOrders.length > 0 && (
                    <Badge size="lg" color="yellow" variant="light">
                        Polling {pendingOrders.length} pending payment {pendingOrders.length === 1 ? "order" : "orders"}
                    </Badge>
                )}
            </Group>

            {asUserId && (
                <Text c="dimmed" size="sm">
                    Explore mode: acting on user {asUserId}
                </Text>
            )}

            <Card withBorder radius="md" p="md">
                <Stack>
                    <Text fw={700}>Create Order</Text>
                    <div
                        style={{
                            display: "grid",
                            gap: 16,
                            gridTemplateColumns: "repeat(auto-fit, minmax(220px, 1fr))",
                        }}
                    >
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
                        {orderType === "physical" ? (
                            <TextInput
                                label="Tracking Number"
                                value={trackingNumber}
                                onChange={(event) => setTrackingNumber(event.currentTarget.value)}
                            />
                        ) : (
                            <TextInput
                                label="Workflow"
                                value="Payment -> Execution"
                                disabled
                            />
                        )}
                    </div>
                    <Group justify="flex-end">
                        <Button onClick={onCreate} loading={isCreating}>
                            Create Order
                        </Button>
                    </Group>
                </Stack>
            </Card>

            <Card withBorder radius="md" p="md">
                <Stack>
                    <Group justify="space-between">
                        <Text fw={700}>Workflow Orders</Text>
                        <Text size="sm" c="dimmed">
                            {orders.length} total
                        </Text>
                    </Group>

                    <Table striped withTableBorder withColumnBorders highlightOnHover>
                        <Table.Thead>
                            <Table.Tr>
                                <Table.Th>Type</Table.Th>
                                <Table.Th>Amount</Table.Th>
                                <Table.Th>Status</Table.Th>
                                <Table.Th>Created</Table.Th>
                                <Table.Th>Actions</Table.Th>
                            </Table.Tr>
                        </Table.Thead>
                        <Table.Tbody>
                            {orders.map((order) => {
                                const meta = getStatusMeta(order.status);
                                const detailsHref = asUserId
                                    ? `/orders/${order.id}?asUserId=${encodeURIComponent(asUserId)}`
                                    : `/orders/${order.id}`;

                                return (
                                    <Table.Tr key={order.id}>
                                        <Table.Td>{order.orderType}</Table.Td>
                                        <Table.Td>${order.totalAmount.toFixed(2)}</Table.Td>
                                        <Table.Td>
                                            <Badge color={meta.color} variant="light">
                                                {meta.label}
                                            </Badge>
                                        </Table.Td>
                                        <Table.Td>{new Date(order.createdAtUtc).toLocaleString()}</Table.Td>
                                        <Table.Td>
                                            <Group gap="xs">
                                                <Button
                                                    component={Link}
                                                    to={detailsHref}
                                                    size="xs"
                                                    variant="light"
                                                >
                                                    View
                                                </Button>
                                                <Button
                                                    size="xs"
                                                    color="red"
                                                    variant="light"
                                                    loading={isDeleting}
                                                    onClick={() => {
                                                        void onDelete(order.id);
                                                    }}
                                                >
                                                    Delete
                                                </Button>
                                            </Group>
                                        </Table.Td>
                                    </Table.Tr>
                                );
                            })}
                        </Table.Tbody>
                    </Table>
                </Stack>
            </Card>
        </Stack>
    );
}
