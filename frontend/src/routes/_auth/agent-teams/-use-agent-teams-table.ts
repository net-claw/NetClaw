import {
  type PaginationState,
  type RowSelectionState,
  type SortingState,
} from "@tanstack/react-table"
import { useDeferredValue, useMemo, useState } from "react"

import { useDataTable } from "@/components/data-table/use-data-table"
import {
  useDeleteAgentTeams,
  useGetAgentTeamList,
} from "@/hooks/api/use-agent-team"
import { getAgentTeamColumns } from "@/routes/_auth/agent-teams/-columns"
import { useTranslation } from "react-i18next"

const pageSizeOptions = [10, 20, 50] as const

export function useAgentTeamsTable(query: string) {
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
  const deleteAgentTeamsMutation = useDeleteAgentTeams()

  const orderBy = "name"
  const ascending = sorting[0] ? !sorting[0].desc : true

  const { data, isLoading, isError, isFetching, refetch } = useGetAgentTeamList({
    pageIndex: pagination.pageIndex,
    pageSize: pagination.pageSize,
    searchText: deferredQuery || undefined,
    orderBy,
    ascending,
  })

  const teams = useMemo(() => data?.items ?? [], [data?.items])
  const totalItems = data?.totalItems ?? 0
  const currentIds = useMemo(() => new Set(teams.map((team) => team.id)), [teams])
  const visibleRowSelection = useMemo(
    () =>
      Object.fromEntries(
        Object.entries(rowSelection).filter(
          ([id, selected]) => selected && currentIds.has(id)
        )
      ),
    [currentIds, rowSelection]
  )

  const columns = useMemo(() => getAgentTeamColumns({ t, sorting }), [sorting, t])

  const table = useDataTable({
    data: teams,
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

  const selectedTeamIds = Object.keys(visibleRowSelection).filter(
    (id) => visibleRowSelection[id]
  )

  const handleDeleteSelected = async () => {
    if (selectedTeamIds.length === 0) {
      return
    }

    try {
      await deleteAgentTeamsMutation.mutateAsync(selectedTeamIds)
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
    selectedTeamIds,
    handleDeleteSelected,
    deleteAgentTeamsMutation,
    pageSizeOptions,
    setPagination,
  }
}
