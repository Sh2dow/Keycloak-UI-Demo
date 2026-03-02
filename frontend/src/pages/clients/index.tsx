import { useList } from "@refinedev/core";
import { Loader, Stack, Table, Text, Title } from "@mantine/core";

type ClientRecord = {
    id: string;
    clientId: string;
    name: string;
};

export function ClientsPage() {
    const listQuery = useList<ClientRecord>({
        resource: "clients",
    });

    if (listQuery.isLoading) {
        return <Loader />;
    }

    const clients: ClientRecord[] = listQuery.data?.data ?? [];

    return (
        <Stack>
            <Title order={2}>Clients</Title>
            {clients.length === 0 ? (
                <Text c="dimmed">No client audience claims found.</Text>
            ) : (
                <Table striped withTableBorder withColumnBorders>
                    <Table.Thead>
                        <Table.Tr>
                            <Table.Th>Client ID</Table.Th>
                        </Table.Tr>
                    </Table.Thead>
                    <Table.Tbody>
                        {clients.map((client) => (
                            <Table.Tr key={client.id}>
                                <Table.Td>{client.clientId}</Table.Td>
                            </Table.Tr>
                        ))}
                    </Table.Tbody>
                </Table>
            )}
        </Stack>
    );
}
