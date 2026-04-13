import { Link } from "@tanstack/react-router"
import { useMemo, useState } from "react"

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
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import {
  useDeleteAgentTeams,
  useGetAgentTeamList,
} from "@/hooks/api/use-agent-team"
import { actionIcons, appIcons } from "@/lib/icons"
import { useTranslation } from "react-i18next"

export default function AgentTeamsListPage() {
  const { t } = useTranslation()
  const [query, setQuery] = useState("")
  const [selectedIds, setSelectedIds] = useState<string[]>([])
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false)
  const TeamsIcon = appIcons.agents
  const CreateIcon = actionIcons.create
  const DeleteIcon = actionIcons.delete
  const SearchIcon = actionIcons.search
  const RefreshIcon = actionIcons.refresh
  const deleteAgentTeamsMutation = useDeleteAgentTeams()

  const { data, isLoading, isFetching, refetch } = useGetAgentTeamList({
    pageIndex: 0,
    pageSize: 50,
    searchText: query.trim() || undefined,
    orderBy: "name",
    ascending: true,
  })

  const teams = data?.items ?? []
  const totalItems = data?.totalItems ?? 0
  const selectedIdSet = useMemo(() => new Set(selectedIds), [selectedIds])

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
          selectedIds.length > 0 ? (
            <DeleteConfirmDialog
              open={isDeleteDialogOpen}
              onOpenChange={setIsDeleteDialogOpen}
              title={t("agentTeams.deleteDialog.title")}
              description={t("agentTeams.deleteDialog.description_other", {
                count: selectedIds.length,
              })}
              confirmLabel={t("agentTeams.deleteDialog.confirm")}
              cancelLabel={t("agentTeams.deleteDialog.cancel")}
              onConfirm={async () => {
                await deleteAgentTeamsMutation.mutateAsync(selectedIds)
                setSelectedIds([])
                setIsDeleteDialogOpen(false)
              }}
              trigger={
                <Button type="button" size="sm" variant="destructive">
                  <DeleteIcon data-icon="inline-start" />
                  {t("agentTeams.actions.deleteSelected_other", {
                    count: selectedIds.length,
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
                  onChange={(event) => setQuery(event.target.value)}
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
              {t("identity.toolbar.refresh")}
            </Button>
          </div>

          <Table>
            <TableHeader>
              <TableRow>
                <TableHead />
                <TableHead>{t("agentTeams.table.name")}</TableHead>
                <TableHead>{t("agentTeams.table.status")}</TableHead>
                <TableHead>Members</TableHead>
                <TableHead>Root member</TableHead>
                <TableHead>{t("agentTeams.table.actions")}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {teams.map((team) => {
                const rootMembers = team.members
                  .filter((member) => !member.reportsToMemberId)
                  .sort((left, right) => left.order - right.order)

                return (
                  <TableRow key={team.id}>
                    <TableCell>
                      <input
                        type="checkbox"
                        checked={selectedIdSet.has(team.id)}
                        onChange={(event) => {
                          setSelectedIds((current) =>
                            event.target.checked
                              ? [...current, team.id]
                              : current.filter((id) => id !== team.id)
                          )
                        }}
                      />
                    </TableCell>
                    <TableCell className="font-medium">
                      <div>{team.name}</div>
                      <div className="text-xs text-muted-foreground">
                        {team.description || "-"}
                      </div>
                    </TableCell>
                    <TableCell>{team.status}</TableCell>
                    <TableCell>{team.members.length}</TableCell>
                    <TableCell>
                      {rootMembers.length > 0
                        ? `${rootMembers[0].agentName ?? rootMembers[0].agentId}${rootMembers.length > 1 ? ` +${rootMembers.length - 1}` : ""}`
                        : "-"}
                    </TableCell>
                    <TableCell>
                      <Button asChild size="sm" variant="outline">
                        <Link
                          to="/agent-teams/$teamId/edit"
                          params={{ teamId: team.id }}
                        >
                          {t("identity.actions.edit")}
                        </Link>
                      </Button>
                    </TableCell>
                  </TableRow>
                )
              })}

              {!isLoading && teams.length === 0 ? (
                <TableRow>
                  <TableCell
                    colSpan={6}
                    className="text-center text-sm text-muted-foreground"
                  >
                    {t("agentTeams.emptyAgentTeams")}
                  </TableCell>
                </TableRow>
              ) : null}
            </TableBody>
          </Table>
        </div>
      </SectionCard>
    </>
  )
}
