import {
  type PaginationState,
  type RowSelectionState,
  type SortingState,
} from "@tanstack/react-table"
import { useDeferredValue, useMemo, useState } from "react"

import { useDataTable } from "@/components/data-table/use-data-table"
import { useDeleteProviders, useGetProviderList } from "@/hooks/api/use-provider"
import { getProviderColumns } from "@/routes/_auth/providers/-columns"
import { useTranslation } from "react-i18next"

const pageSizeOptions = [10, 20, 50] as const

export function useProvidersTable(query: string) {
  const { t } = useTranslation()
  const deferredQuery = useDeferredValue(query.trim())
  const [pagination, setPagination] = useState<PaginationState>({
    pageIndex: 0,
    pageSize: 10,
  })
  const [sorting, setSorting] = useState<SortingState>([
    { id: "updatedAt", desc: true },
  ])
  const [rowSelection, setRowSelection] = useState<RowSelectionState>({})
  const deleteProvidersMutation = useDeleteProviders()

  const orderBy = sorting[0]?.id === "name" ? "name" : "updatedAt"
  const ascending = sorting[0] ? !sorting[0].desc : false

  const { data, isLoading, isError, isFetching, refetch } = useGetProviderList({
    pageIndex: pagination.pageIndex,
    pageSize: pagination.pageSize,
    searchText: deferredQuery || undefined,
    orderBy,
    ascending,
  })

  const providers = useMemo(() => data?.items ?? [], [data?.items])
  const totalItems = data?.totalItems ?? 0
  const currentIds = useMemo(
    () => new Set(providers.map((provider) => provider.id)),
    [providers]
  )
  const visibleRowSelection = useMemo(
    () =>
      Object.fromEntries(
        Object.entries(rowSelection).filter(
          ([id, selected]) => selected && currentIds.has(id)
        )
      ),
    [currentIds, rowSelection]
  )

  const columns = useMemo(() => getProviderColumns({ t, sorting }), [sorting, t])

  const table = useDataTable({
    data: providers,
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

  const selectedProviderIds = Object.keys(visibleRowSelection).filter(
    (id) => visibleRowSelection[id]
  )

  const handleDeleteSelected = async () => {
    if (selectedProviderIds.length === 0) {
      return
    }

    try {
      await deleteProvidersMutation.mutateAsync(selectedProviderIds)
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
    selectedProviderIds,
    handleDeleteSelected,
    deleteProvidersMutation,
    pageSizeOptions,
    setPagination,
  }
}
