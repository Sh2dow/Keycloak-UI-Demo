export type OrderItem = {
    id: string;
    orderType: "digital" | "physical";
    totalAmount: number;
    status: string;
    createdAtUtc: string;
    downloadUrl?: string | null;
    shippingAddress?: string | null;
    trackingNumber?: string | null;
};

export const PAYMENT_PENDING = "PaymentPending";
export const PAYMENT_AUTHORIZED = "PaymentAuthorized";
export const PAYMENT_FAILED = "PaymentFailed";
export const EXECUTION_DISPATCHED = "ExecutionDispatched";
export const EXECUTION_STARTED = "ExecutionStarted";
export const EXECUTION_COMPLETED = "ExecutionCompleted";
export const EXECUTION_FAILED = "ExecutionFailed";

export const paymentPendingStatuses = new Set([PAYMENT_PENDING]);

const statusMeta: Record<string, { color: string; label: string; paymentState: string; executionState: string }> = {
    [PAYMENT_PENDING]: {
        color: "yellow",
        label: "Awaiting Payment",
        paymentState: "Payment authorization is in progress.",
        executionState: "Execution has not started yet.",
    },
    [PAYMENT_AUTHORIZED]: {
        color: "teal",
        label: "Payment Authorized",
        paymentState: "Payment was authorized successfully.",
        executionState: "Order is waiting to be dispatched for execution.",
    },
    [PAYMENT_FAILED]: {
        color: "red",
        label: "Payment Failed",
        paymentState: "Payment was rejected by the payment workflow.",
        executionState: "Execution is blocked until payment succeeds.",
    },
    [EXECUTION_DISPATCHED]: {
        color: "blue",
        label: "Execution Dispatched",
        paymentState: "Payment is complete for this workflow stage.",
        executionState: "Order execution has been dispatched.",
    },
    [EXECUTION_STARTED]: {
        color: "cyan",
        label: "Execution Started",
        paymentState: "Payment is complete for this workflow stage.",
        executionState: "Order execution is in progress.",
    },
    [EXECUTION_COMPLETED]: {
        color: "green",
        label: "Execution Completed",
        paymentState: "Payment is complete for this workflow stage.",
        executionState: "Order execution finished successfully.",
    },
    [EXECUTION_FAILED]: {
        color: "red",
        label: "Execution Failed",
        paymentState: "Payment was already completed.",
        executionState: "Order execution failed and may need manual review.",
    },
};

export function getStatusMeta(status: string) {
    return statusMeta[status] ?? {
        color: "gray",
        label: status,
        paymentState: "Unknown payment state.",
        executionState: "Unknown execution state.",
    };
}

export function buildTimeline(order: OrderItem) {
    return [
        {
            key: "created",
            label: "Order created",
            description: `Created ${new Date(order.createdAtUtc).toLocaleString()}`,
            active: true,
        },
        {
            key: "payment-pending",
            label: "Payment requested",
            description: "Order entered the payment workflow.",
            active: true,
        },
        {
            key: "payment-authorized",
            label: "Payment authorized",
            description: "Payment service accepted the order.",
            active: order.status === PAYMENT_AUTHORIZED || order.status === EXECUTION_DISPATCHED,
        },
        {
            key: "payment-failed",
            label: "Payment failed",
            description: "Payment service rejected the order.",
            active: order.status === PAYMENT_FAILED,
        },
        {
            key: "execution-dispatched",
            label: "Execution dispatched",
            description: "Order execution was dispatched after payment success.",
            active: order.status === EXECUTION_DISPATCHED,
        },
    ];
}
