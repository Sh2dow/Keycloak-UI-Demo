import { useList } from "@refinedev/core";
import { Badge, Loader, Stack, Table, Text, Title } from "@mantine/core";

type RoleRecord = {
    id: string;
    name: string;
};

export function RolesPage() {
    const listQuery = useList<RoleRecord>({
        resource: "roles",
    });

    if (listQuery.isLoading) {
        return <Loader />;
    }

    const roles: RoleRecord[] = listQuery.data?.data ?? [];

    return (
        <Stack>
            <Title order={2}>Roles</Title>
            {roles.length === 0 ? (
                <Text c="dimmed">No roles found in current token/session.</Text>
            ) : (
                <Table striped withTableBorder withColumnBorders>
                    <Table.Thead>
                        <Table.Tr>
                            <Table.Th>Role Name</Table.Th>
                            <Table.Th>Preview</Table.Th>
                        </Table.Tr>
                    </Table.Thead>
                    <Table.Tbody>
                        {roles.map((role) => (
                            <Table.Tr key={role.id}>
                                <Table.Td>{role.name}</Table.Td>
                                <Table.Td>
                                    <Badge>{role.name}</Badge>
                                </Table.Td>
                            </Table.Tr>
                        ))}
                    </Table.Tbody>
                </Table>
            )}
        </Stack>
    );
}
