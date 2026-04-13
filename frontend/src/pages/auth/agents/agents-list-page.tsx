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
import { useDeleteAgents, useGetAgentList } from "@/hooks/api/use-agent"
import { actionIcons, appIcons } from "@/lib/icons"
import { useTranslation } from "react-i18next"

export default function AgentsListPage() {
  const { t } = useTranslation()
  const [query, setQuery] = useState("")
  const [selectedIds, setSelectedIds] = useState<string[]>([])
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false)
  const AgentsIcon = appIcons.agents
  const CreateIcon = actionIcons.create
  const DeleteIcon = actionIcons.delete
  const SearchIcon = actionIcons.search
  const RefreshIcon = actionIcons.refresh
  const deleteAgentsMutation = useDeleteAgents()

  const { data, isLoading, isFetching, refetch } = useGetAgentList({
    pageIndex: 0,
    pageSize: 50,
    searchText: query.trim() || undefined,
    orderBy: "name",
    ascending: true,
  })

  const agents = data?.items ?? []
  const totalItems = data?.totalItems ?? 0
  const selectedIdSet = useMemo(() => new Set(selectedIds), [selectedIds])

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
          selectedIds.length > 0 ? (
            <DeleteConfirmDialog
              open={isDeleteDialogOpen}
              onOpenChange={setIsDeleteDialogOpen}
              title={t("agents.deleteDialog.title")}
              description={t("agents.deleteDialog.description_other", {
                count: selectedIds.length,
              })}
              confirmLabel={t("agents.deleteDialog.confirm")}
              cancelLabel={t("agents.deleteDialog.cancel")}
              onConfirm={async () => {
                await deleteAgentsMutation.mutateAsync(selectedIds)
                setSelectedIds([])
                setIsDeleteDialogOpen(false)
              }}
              trigger={
                <Button type="button" size="sm" variant="destructive">
                  <DeleteIcon data-icon="inline-start" />
                  {t("agents.actions.deleteSelected_other", {
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
                  placeholder={t("agents.searchPlaceholder")}
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
                <TableHead>{t("agents.table.name")}</TableHead>
                <TableHead>{t("agents.table.role")}</TableHead>
                <TableHead>{t("agents.table.kind")}</TableHead>
                <TableHead>{t("agents.table.type")}</TableHead>
                <TableHead>{t("agents.table.provider")}</TableHead>
                <TableHead>{t("agents.table.actions")}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {agents.map((agent) => (
                <TableRow key={agent.id}>
                  <TableCell>
                    <input
                      type="checkbox"
                      checked={selectedIdSet.has(agent.id)}
                      onChange={(event) => {
                        setSelectedIds((current) =>
                          event.target.checked
                            ? [...current, agent.id]
                            : current.filter((id) => id !== agent.id)
                        )
                      }}
                    />
                  </TableCell>
                  <TableCell className="font-medium">
                    <div>{agent.name}</div>
                    <div className="text-xs text-muted-foreground">
                      {agent.status}
                    </div>
                  </TableCell>
                  <TableCell>{agent.role}</TableCell>
                  <TableCell>{agent.kind}</TableCell>
                  <TableCell>{agent.type}</TableCell>
                  <TableCell>
                    {agent.providers.length > 0
                      ? `${agent.providers[0].name}${agent.providers.length > 1 ? ` +${agent.providers.length - 1}` : ""}`
                      : "-"}
                  </TableCell>
                  <TableCell>
                    <Button asChild size="sm" variant="outline">
                      <Link to="/agents/$agentId/edit" params={{ agentId: agent.id }}>
                        {t("identity.actions.edit")}
                      </Link>
                    </Button>
                  </TableCell>
                </TableRow>
              ))}

              {!isLoading && agents.length === 0 ? (
                <TableRow>
                  <TableCell
                    colSpan={7}
                    className="text-center text-sm text-muted-foreground"
                  >
                    {t("agents.emptyAgents")}
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
