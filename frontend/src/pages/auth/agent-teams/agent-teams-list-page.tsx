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
import { useAgentTeamsTable } from "@/routes/_auth/agent-teams/-use-agent-teams-table"
import { useTranslation } from "react-i18next"

export default function AgentTeamsListPage() {
  const { t } = useTranslation()
  const [query, setQuery] = useState("")
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false)
  const TeamsIcon = appIcons.agents
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
    selectedTeamIds,
    handleDeleteSelected,
    deleteAgentTeamsMutation,
    pageSizeOptions,
    setPagination,
  } = useAgentTeamsTable(query)

  return (
    <>
      <PageHeaderCard
        icon={<TeamsIcon />}
        title={t("agentTeams.page.title")}
        description={t("agentTeams.page.description")}
        titleMeta={totalItems}
        headerRight={
          <Button asChild size="lg">
            <Link to="/agent-teams/create">
              <CreateIcon data-icon="inline-start" />
              {t("agentTeams.actions.newAgentTeam")}
            </Link>
          </Button>
        }
      />

      <SectionCard
        headerRight={
          selectedTeamIds.length > 0 ? (
            <DeleteConfirmDialog
              open={isDeleteDialogOpen}
              onOpenChange={setIsDeleteDialogOpen}
              title={t("agentTeams.deleteDialog.title")}
              description={t("agentTeams.deleteDialog.description_other", {
                count: selectedTeamIds.length,
              })}
              confirmLabel={t("agentTeams.deleteDialog.confirm")}
              cancelLabel={t("agentTeams.deleteDialog.cancel")}
              onConfirm={async () => {
                await handleDeleteSelected()
                setIsDeleteDialogOpen(false)
              }}
              trigger={
                <Button
                  type="button"
                  size="sm"
                  variant="destructive"
                  disabled={deleteAgentTeamsMutation.isPending}
                >
                  <DeleteIcon data-icon="inline-start" />
                  {t("agentTeams.actions.deleteSelected_other", {
                    count: selectedTeamIds.length,
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
                  placeholder={t("agentTeams.searchPlaceholder")}
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
              {t("agentTeams.errors.loadAgentTeams")}
            </div>
          ) : (
            <>
              <DataTable
                table={table}
                loading={isLoading}
                emptyMessage={t("agentTeams.emptyAgentTeams")}
              />
              <DataTablePagination
                table={table}
                rowCount={totalItems}
                isFetching={isFetching && !isLoading}
                pageSizeOptions={pageSizeOptions}
                selectedCount={selectedTeamIds.length}
              />
            </>
          )}
        </div>
      </SectionCard>
    </>
  )
}
