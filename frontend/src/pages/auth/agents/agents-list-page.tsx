import { Link } from "@tanstack/react-router"
import { useState } from "react"

import { DataTable } from "@/components/data-table/data-table"
import { DataTablePagination } from "@/components/data-table/data-table-pagination"
import { DeleteConfirmDialog } from "@/components/delete-confirm-dialog"
import { PageHeaderCard } from "@/components/share/cards/page-header-card"
import { SectionCard } from "@/components/share/cards/section-card"
import { Button } from "@/components/ui/button"
import {
  InputGroup,
  InputGroupAddon,
  InputGroupInput,
  InputGroupText,
} from "@/components/ui/input-group"
import { actionIcons, appIcons } from "@/lib/icons"
import { useAgentsTable } from "@/routes/_auth/agents/-use-agents-table"
import { useTranslation } from "react-i18next"

export default function AgentsListPage() {
  const { t } = useTranslation()
  const [query, setQuery] = useState("")
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false)
  const AgentsIcon = appIcons.agents
  const CreateIcon = actionIcons.create
  const DeleteIcon = actionIcons.delete
  const SearchIcon = actionIcons.search
  const RefreshIcon = actionIcons.refresh
  const {
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
  } = useAgentsTable(query)

  return (
    <>
      <PageHeaderCard
        icon={<AgentsIcon />}
        title={t("agents.page.title")}
        description={t("agents.page.description")}
        titleMeta={totalItems}
        headerRight={
          <Button asChild size="lg">
            <Link to="/agents/create">
              <CreateIcon data-icon="inline-start" />
              {t("agents.actions.newAgent")}
            </Link>
          </Button>
        }
      />

      <SectionCard
        headerRight={
          selectedAgentIds.length > 0 ? (
            <DeleteConfirmDialog
              open={isDeleteDialogOpen}
              onOpenChange={setIsDeleteDialogOpen}
              title={t("agents.deleteDialog.title")}
              description={t("agents.deleteDialog.description_other", {
                count: selectedAgentIds.length,
              })}
              confirmLabel={t("agents.deleteDialog.confirm")}
              cancelLabel={t("agents.deleteDialog.cancel")}
              onConfirm={async () => {
                await handleDeleteSelected()
                setIsDeleteDialogOpen(false)
              }}
              trigger={
                <Button
                  type="button"
                  size="sm"
                  variant="destructive"
                  disabled={deleteAgentsMutation.isPending}
                >
                  <DeleteIcon data-icon="inline-start" />
                  {t("agents.actions.deleteSelected_other", {
                    count: selectedAgentIds.length,
                  })}
                </Button>
              }
            />
          ) : null
        }
      >
        <div className="flex flex-col gap-4">
          <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
            <div className="w-full max-w-xl">
              <InputGroup>
                <InputGroupAddon>
                  <InputGroupText>
                    <SearchIcon />
                  </InputGroupText>
                </InputGroupAddon>
                <InputGroupInput
                  value={query}
                  placeholder={t("agents.searchPlaceholder")}
                  onChange={(event) => {
                    setQuery(event.target.value)
                    setPagination((current) => ({ ...current, pageIndex: 0 }))
                  }}
                />
              </InputGroup>
            </div>

            <Button
              type="button"
              variant="outline"
              size="sm"
              disabled={isFetching}
              onClick={() => {
                void refetch()
              }}
            >
              <RefreshIcon data-icon="inline-start" />
              {t("common.toolbar.refresh")}
            </Button>
          </div>

          {isError ? (
            <div className="rounded-xl border border-destructive/20 bg-destructive/5 px-4 py-3 text-sm text-destructive">
              {t("agents.errors.loadAgents")}
            </div>
          ) : (
            <>
              <DataTable
                table={table}
                loading={isLoading}
                emptyMessage={t("agents.emptyAgents")}
              />
              <DataTablePagination
                table={table}
                rowCount={totalItems}
                isFetching={isFetching && !isLoading}
                pageSizeOptions={pageSizeOptions}
                selectedCount={selectedAgentIds.length}
              />
            </>
          )}
        </div>
      </SectionCard>
    </>
  )
}
