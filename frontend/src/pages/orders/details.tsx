import { useEffect, useMemo } from "react";
import { useCustom, useCustomMutation } from "@refinedev/core";
import { Alert, Anchor, Badge, Button, Card, Divider, Group, Loader, SimpleGrid, Stack, Text, Title } from "@mantine/core";
import { Link, useSearchParams, useParams } from "react-router-dom";
import { EXECUTION_FAILED, getStatusMeta, PAYMENT_FAILED, paymentPendingStatuses, type OrderItem } from "./shared";

type OrderPaymentEvent = {
    attemptNumber: number;
    sequenceNumber: number;
    eventType: string;
    occurredAtUtc: string;
    description: string;
    reason?: string | null;
};

type OrderPaymentDetails = {
    orderId: string;
    paymentId?: string | null;
    currentAttemptNumber: number;
    orderStatus: string;
    sagaState: string;
    paymentState: string;
    createdAtUtc: string;
    lastPaymentRequestedAtUtc?: string | null;
    lastPaymentCompletedAtUtc?: string | null;
    executionDispatchedAtUtc?: string | null;
    executionStartedAtUtc?: string | null;
    executionCompletedAtUtc?: string | null;
    executionFailedAtUtc?: string | null;
    executionFailureReason?: string | null;
    failureReason?: string | null;
    events: OrderPaymentEvent[];
};

type OrderTimelineItem = {
    key: string;
    label: string;
    state: string;
    occurredAtUtc?: string | null;
    description: string;
};

type OrderWorkflow = {
    order: OrderItem;
    payment: OrderPaymentDetails;
    timeline: OrderTimelineItem[];
};

export function OrderDetailsPage() {
    const [searchParams] = useSearchParams();
    const { id } = useParams<{ id: string }>();
    const asUserId = searchParams.get("asUserId") ?? undefined;
    const retryPaymentMutation = useCustomMutation();
    const workflowQuery = useCustom<OrderWorkflow>({
        url: id ? `/api/orders/${id}/workflow` : "/api/orders/invalid/workflow",
        method: "get",
        meta: asUserId ? { asUserId } : undefined,
        queryOptions: {
            enabled: Boolean(id),
        },
    });

    const workflow = workflowQuery.data?.data;
    const order = workflow?.order;
    const paymentDetails = workflow?.payment;
    const timeline = workflow?.timeline ?? [];
    const statusMeta = useMemo(() => (paymentDetails ? getStatusMeta(paymentDetails.orderStatus) : null), [paymentDetails]);

    useEffect(() => {
        const currentStatus = paymentDetails?.orderStatus;
        if (!currentStatus || !paymentPendingStatuses.has(currentStatus)) {
            return undefined;
        }

        const timer = window.setInterval(() => {
            void workflowQuery.refetch();
        }, 3000);

        return () => {
            window.clearInterval(timer);
        };
    }, [paymentDetails, workflowQuery]);

    if (!id) {
        return (
            <Alert color="red" title="Missing order id">
                Open this page from the orders list so a valid order can be loaded.
            </Alert>
        );
    }

    if (workflowQuery.isLoading) {
        return <Loader />;
    }

    if (workflowQuery.isError || !order || !paymentDetails || !statusMeta) {
        return (
            <Stack>
                <Button component={Link} to="/orders" variant="subtle" w="fit-content">
                    Back to Orders
                </Button>
                <Alert color="red" title="Order could not be loaded">
                    {workflowQuery.error instanceof Error ? workflowQuery.error.message : "The order details request failed."}
                </Alert>
            </Stack>
        );
    }

    return (
        <Stack>
            <Group justify="space-between" align="flex-start">
                <div>
                    <Button component={Link} to={asUserId ? `/orders?asUserId=${encodeURIComponent(asUserId)}` : "/orders"} variant="subtle" px={0}>
                        Back to Orders
                    </Button>
                    <Title order={2}>Order Details</Title>
                    <Text c="dimmed" size="sm">
                        Workflow view for order {order.id}
                    </Text>
                </div>
                <Badge color={statusMeta.color} variant="light" size="lg">
                    {statusMeta.label}
                </Badge>
            </Group>

            {asUserId && (
                <Text c="dimmed" size="sm">
                    Explore mode: acting on user {asUserId}
                </Text>
            )}

            {paymentDetails.orderStatus === PAYMENT_FAILED && (
                <Alert color="red" title="Payment failed">
                    <Stack gap="sm">
                        <Text>{paymentDetails.failureReason ?? "The payment workflow rejected this order."}</Text>
                        <Group>
                            <Button
                                size="xs"
                                loading={retryPaymentMutation.isLoading}
                                onClick={() => {
                                    if (!id) {
                                        return;
                                    }

                                    retryPaymentMutation.mutate(
                                        {
                                            url: `/api/orders/${id}/retry-payment`,
                                            method: "post",
                                            values: {},
                                            meta: asUserId ? { asUserId } : undefined,
                                        },
                                        {
                                            onSuccess: () => {
                                                void workflowQuery.refetch();
                                            },
                                        },
                                    );
                                }}
                            >
                                Retry Payment
                            </Button>
                        </Group>
                    </Stack>
                </Alert>
            )}

            {paymentDetails.orderStatus === EXECUTION_FAILED && (
                <Alert color="red" title="Execution failed">
                    {paymentDetails.executionFailureReason ?? "Execution failed after payment authorization."}
                </Alert>
            )}

            <SimpleGrid cols={{ base: 1, xl: 2 }}>
                <Card withBorder radius="md" p="md">
                    <Stack>
                        <Text fw={700}>Workflow Summary</Text>
                        <SimpleGrid cols={{ base: 1, sm: 2 }}>
                            <div>
                                <Text size="sm" c="dimmed">Type</Text>
                                <Text>{order.orderType}</Text>
                            </div>
                            <div>
                                <Text size="sm" c="dimmed">Amount</Text>
                                <Text>${order.totalAmount.toFixed(2)}</Text>
                            </div>
                            <div>
                                <Text size="sm" c="dimmed">Created</Text>
                                <Text>{new Date(order.createdAtUtc).toLocaleString()}</Text>
                            </div>
                            <div>
                                <Text size="sm" c="dimmed">Saga State</Text>
                                <Text>{paymentDetails.sagaState}</Text>
                            </div>
                            <div>
                                <Text size="sm" c="dimmed">Payment State</Text>
                                <Text>{paymentDetails.paymentState}</Text>
                            </div>
                            <div>
                                <Text size="sm" c="dimmed">Execution State</Text>
                                <Text>{statusMeta.executionState}</Text>
                            </div>
                            <div>
                                <Text size="sm" c="dimmed">Payment Id</Text>
                                <Text>{paymentDetails.paymentId ?? "Not assigned yet"}</Text>
                            </div>
                            <div>
                                <Text size="sm" c="dimmed">Current Attempt</Text>
                                <Text>{paymentDetails.currentAttemptNumber || 0}</Text>
                            </div>
                        </SimpleGrid>

                        <Divider />

                        <SimpleGrid cols={{ base: 1, sm: 2 }}>
                            <div>
                                <Text size="sm" c="dimmed">Last Payment Requested</Text>
                                <Text>
                                    {paymentDetails.lastPaymentRequestedAtUtc
                                        ? new Date(paymentDetails.lastPaymentRequestedAtUtc).toLocaleString()
                                        : "Not recorded"}
                                </Text>
                            </div>
                            <div>
                                <Text size="sm" c="dimmed">Last Payment Completed</Text>
                                <Text>
                                    {paymentDetails.lastPaymentCompletedAtUtc
                                        ? new Date(paymentDetails.lastPaymentCompletedAtUtc).toLocaleString()
                                        : "Not recorded"}
                                </Text>
                            </div>
                            <div>
                                <Text size="sm" c="dimmed">Execution Dispatched</Text>
                                <Text>
                                    {paymentDetails.executionDispatchedAtUtc
                                        ? new Date(paymentDetails.executionDispatchedAtUtc).toLocaleString()
                                        : "Not dispatched"}
                                </Text>
                            </div>
                            <div>
                                <Text size="sm" c="dimmed">Execution Started</Text>
                                <Text>
                                    {paymentDetails.executionStartedAtUtc
                                        ? new Date(paymentDetails.executionStartedAtUtc).toLocaleString()
                                        : "Not started"}
                                </Text>
                            </div>
                            <div>
                                <Text size="sm" c="dimmed">Execution Completed</Text>
                                <Text>
                                    {paymentDetails.executionCompletedAtUtc
                                        ? new Date(paymentDetails.executionCompletedAtUtc).toLocaleString()
                                        : "Not completed"}
                                </Text>
                            </div>
                            <div>
                                <Text size="sm" c="dimmed">Execution Failed</Text>
                                <Text>
                                    {paymentDetails.executionFailedAtUtc
                                        ? new Date(paymentDetails.executionFailedAtUtc).toLocaleString()
                                        : "No failure recorded"}
                                </Text>
                            </div>
                        </SimpleGrid>

                        <Divider />

                        <div>
                            <Text fw={600}>Order Payload</Text>
                            {order.orderType === "digital" ? (
                                order.downloadUrl ? (
                                    <Anchor href={order.downloadUrl} target="_blank" rel="noreferrer" size="sm">
                                        {order.downloadUrl}
                                    </Anchor>
                                ) : (
                                    <Text size="sm" c="dimmed">No download URL</Text>
                                )
                            ) : (
                                <>
                                    <Text size="sm" c="dimmed">
                                        {order.shippingAddress ?? "No shipping address"}
                                    </Text>
                                    <Text size="sm" c="dimmed">
                                        Tracking: {order.trackingNumber ?? "not assigned"}
                                    </Text>
                                </>
                            )}
                        </div>
                    </Stack>
                </Card>

                <Card withBorder radius="md" p="md">
                    <Stack>
                        <Text fw={700}>Workflow Timeline</Text>
                        {timeline.map((step) => (
                            <Group key={step.key} align="flex-start" wrap="nowrap">
                                <Badge
                                    mt={2}
                                    color={step.state === "Completed" ? "blue" : "gray"}
                                    variant={step.state === "Completed" ? "filled" : "outline"}
                                >
                                    {step.state}
                                </Badge>
                                <div>
                                    <Text size="sm" fw={600}>
                                        {step.label}
                                    </Text>
                                    <Text size="sm" c="dimmed">
                                        {step.description}
                                    </Text>
                                    {step.occurredAtUtc && (
                                        <Text size="xs" c="dimmed">
                                            {new Date(step.occurredAtUtc).toLocaleString()}
                                        </Text>
                                    )}
                                </div>
                            </Group>
                        ))}
                    </Stack>
                </Card>
            </SimpleGrid>

            <Card withBorder radius="md" p="md">
                <Stack>
                    <Text fw={700}>Payment Events</Text>
                    {paymentDetails.events.length === 0 ? (
                        <Text c="dimmed" size="sm">
                            No payment events have been recorded yet.
                        </Text>
                    ) : (
                        paymentDetails.events.map((event) => (
                            <Group key={`${event.sequenceNumber}-${event.eventType}`} align="flex-start" wrap="nowrap">
                                <Badge color="dark" variant="light">
                                    A{event.attemptNumber} #{event.sequenceNumber}
                                </Badge>
                                <div>
                                    <Text size="sm" fw={600}>
                                        {event.eventType}
                                    </Text>
                                    <Text size="sm" c="dimmed">
                                        {event.description}
                                    </Text>
                                    {event.reason && (
                                        <Text size="sm" c="red">
                                            Reason: {event.reason}
                                        </Text>
                                    )}
                                    <Text size="xs" c="dimmed">
                                        {new Date(event.occurredAtUtc).toLocaleString()}
                                    </Text>
                                </div>
                            </Group>
                        ))
                    )}
                </Stack>
            </Card>
        </Stack>
    );
}
