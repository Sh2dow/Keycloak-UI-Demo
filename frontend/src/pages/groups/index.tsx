import { useList } from "@refinedev/core";
import { Loader, Stack, Table, Text, Title } from "@mantine/core";

type GroupRecord = {
    id: string;
    name: string;
};

export function GroupsPage() {
    const listQuery = useList<GroupRecord>({
        resource: "groups",
    });

    if (listQuery.isLoading) {
        return <Loader />;
    }

    const groups: GroupRecord[] = listQuery.data?.data ?? [];

    return (
        <Stack>
            <Title order={2}>Groups</Title>
            {groups.length === 0 ? (
                <Text c="dimmed">No group claims in current token.</Text>
            ) : (
                <Table striped withTableBorder withColumnBorders>
                    <Table.Thead>
                        <Table.Tr>
                            <Table.Th>Group</Table.Th>
                        </Table.Tr>
                    </Table.Thead>
                    <Table.Tbody>
                        {groups.map((group) => (
                            <Table.Tr key={group.id}>
                                <Table.Td>{group.name}</Table.Td>
                            </Table.Tr>
                        ))}
                    </Table.Tbody>
                </Table>
            )}
        </Stack>
    );
}
