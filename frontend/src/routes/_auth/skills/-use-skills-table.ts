import {
  type PaginationState,
  type RowSelectionState,
  type SortingState,
} from "@tanstack/react-table"
import { useDeferredValue, useMemo, useState } from "react"

import { useDataTable } from "@/components/data-table/use-data-table"
import { useDeleteSkills, useGetSkillList, useInstallSkill } from "@/hooks/api/use-skill"
import { getSkillColumns } from "@/routes/_auth/skills/-columns"
import { useTranslation } from "react-i18next"

const pageSizeOptions = [10, 20, 50] as const

export function useSkillsTable(query: string) {
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
  const deleteSkillsMutation = useDeleteSkills()
  const installSkillMutation = useInstallSkill()

  const orderBy = sorting[0]?.id === "name" ? "name" : "updatedAt"
  const ascending = sorting[0] ? !sorting[0].desc : false

  const { data, isLoading, isError, isFetching, refetch } = useGetSkillList({
    pageIndex: pagination.pageIndex,
    pageSize: pagination.pageSize,
    searchText: deferredQuery || undefined,
    orderBy,
    ascending,
  })

  const skills = useMemo(() => data?.items ?? [], [data?.items])
  const totalItems = data?.totalItems ?? 0
  const currentIds = useMemo(() => new Set(skills.map((skill) => skill.id)), [skills])
  const visibleRowSelection = useMemo(
    () =>
      Object.fromEntries(
        Object.entries(rowSelection).filter(
          ([id, selected]) => selected && currentIds.has(id)
        )
      ),
    [currentIds, rowSelection]
  )

  const columns = useMemo(
    () =>
      getSkillColumns({
        t,
        sorting,
        installPending: installSkillMutation.isPending,
        onInstall: (skillId) => {
          installSkillMutation.mutate(skillId)
        },
      }),
    [installSkillMutation, sorting, t]
  )

  const table = useDataTable({
    data: skills,
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

  const selectedSkillIds = Object.keys(visibleRowSelection).filter(
    (id) => visibleRowSelection[id]
  )

  const handleDeleteSelected = async () => {
    if (selectedSkillIds.length === 0) {
      return
    }

    try {
      await deleteSkillsMutation.mutateAsync(selectedSkillIds)
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
    selectedSkillIds,
    handleDeleteSelected,
    deleteSkillsMutation,
    pageSizeOptions,
    setPagination,
  }
}
