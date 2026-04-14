import {
  type PaginationState,
  type RowSelectionState,
  type SortingState,
} from "@tanstack/react-table"
import { useDeferredValue, useMemo, useState } from "react"

import { useDataTable } from "@/components/data-table/use-data-table"
import { useDeleteAgents, useGetAgentList } from "@/hooks/api/use-agent"
import { getAgentColumns } from "@/routes/_auth/agents/-columns"
import { useTranslation } from "react-i18next"

const pageSizeOptions = [10, 20, 50] as const

export function useAgentsTable(query: string) {
  const { t } = useTranslation()
  const deferredQuery = useDeferredValue(query.trim())
  const [pagination, setPagination] = useState<PaginationState>({
    pageIndex: 0,
    pageSize: 10,
  })
  const [sorting, setSorting] = useState<SortingState>([
    { id: "name", desc: false },
  ])
  const [rowSelection, setRowSelection] = useState<RowSelectionState>({})
  const deleteAgentsMutation = useDeleteAgents()

  const orderBy = "name"
  const ascending = sorting[0] ? !sorting[0].desc : true

  const { data, isLoading, isError, isFetching, refetch } = useGetAgentList({
    pageIndex: pagination.pageIndex,
    pageSize: pagination.pageSize,
    searchText: deferredQuery || undefined,
    orderBy,
    ascending,
  })

  const agents = useMemo(() => data?.items ?? [], [data?.items])
  const totalItems = data?.totalItems ?? 0
  const currentIds = useMemo(() => new Set(agents.map((agent) => agent.id)), [agents])
  const visibleRowSelection = useMemo(
    () =>
      Object.fromEntries(
        Object.entries(rowSelection).filter(
          ([id, selected]) => selected && currentIds.has(id)
        )
      ),
    [currentIds, rowSelection]
  )

  const columns = useMemo(() => getAgentColumns({ t, sorting }), [sorting, t])

  const table = useDataTable({
    data: agents,
    columns,
    rowCount: totalItems,
    getRowId: (row) => row.id,
    manualPagination: true,
    manualSorting: true,
    enableRowSelection: true,
    onPaginationChange: setPagination,
    onSortingChange: setSorting,
    onRowSelectionChange: setRowSelection,
    state: {
      pagination,
      sorting,
      rowSelection: visibleRowSelection,
    },
  })

  const selectedAgentIds = Object.keys(visibleRowSelection).filter(
    (id) => visibleRowSelection[id]
  )

  const handleDeleteSelected = async () => {
    if (selectedAgentIds.length === 0) {
      return
    }

    try {
      await deleteAgentsMutation.mutateAsync(selectedAgentIds)
      setRowSelection({})
    } catch {
      // Mutation handles its own toast.
    }
  }

  return {
    table,
    totalItems,
    isLoading,
    isError,
    isFetching,
    refetch,
    selectedAgentIds,
    handleDeleteSelected,
    deleteAgentsMutation,
    pageSizeOptions,
    setPagination,
  }
}
