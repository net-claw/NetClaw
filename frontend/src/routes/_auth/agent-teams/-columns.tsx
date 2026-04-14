import { Link } from "@tanstack/react-router"
import type { ColumnDef, SortingState } from "@tanstack/react-table"
import {
  ArrowDownAZIcon,
  ArrowUpAZIcon,
  ChevronsUpDownIcon,
  SquarePenIcon,
} from "lucide-react"

import type { AgentTeamModel } from "@/@types/models"
import { Button } from "@/components/ui/button"
import { Checkbox } from "@/components/ui/checkbox"
import type { TFunction } from "i18next"

type AgentTeamColumnsOptions = {
  t: TFunction
  sorting: SortingState
}

export function getAgentTeamColumns({
  t,
  sorting,
}: AgentTeamColumnsOptions): ColumnDef<AgentTeamModel>[] {
  const nameSorted = sorting[0]?.id === "name" ? sorting[0] : null

  return [
    {
      id: "select",
      enableSorting: false,
      enableHiding: false,
      header: ({ table }) => (
        <Checkbox
          aria-label={t("agentTeams.table.selectAll")}
          checked={table.getIsAllPageRowsSelected()}
          onCheckedChange={(checked) =>
            table.toggleAllPageRowsSelected(checked === true)
          }
        />
      ),
      cell: ({ row }) => (
        <Checkbox
          aria-label={t("agentTeams.table.selectRow", {
            name: row.original.name,
          })}
          checked={row.getIsSelected()}
          onCheckedChange={(checked) => row.toggleSelected(checked === true)}
        />
      ),
    },
    {
      accessorKey: "name",
      header: ({ column }) => (
        <Button
          type="button"
          variant="ghost"
          size="sm"
          className="-ml-2 h-8 px-2 text-xs font-medium uppercase tracking-wide text-muted-foreground hover:bg-transparent hover:text-foreground"
          onClick={() => column.toggleSorting(nameSorted?.desc === false)}
        >
          {t("agentTeams.table.name")}
          {nameSorted ? (
            nameSorted.desc ? (
              <ArrowDownAZIcon data-icon="inline-end" />
            ) : (
              <ArrowUpAZIcon data-icon="inline-end" />
            )
          ) : (
            <ChevronsUpDownIcon data-icon="inline-end" />
          )}
        </Button>
      ),
      cell: ({ row }) => (
        <div>
          <div className="font-medium">{row.original.name}</div>
          <div className="text-xs text-muted-foreground">
            {row.original.description || "-"}
          </div>
        </div>
      ),
    },
    {
      accessorKey: "status",
      header: () => t("agentTeams.table.status"),
    },
    {
      id: "members",
      header: () => t("agentTeams.table.members"),
      cell: ({ row }) => row.original.members.length,
    },
    {
      id: "rootMember",
      header: () => t("agentTeams.table.rootMember"),
      cell: ({ row }) => {
        const rootMembers = row.original.members
          .filter((member) => !member.reportsToMemberId)
          .sort((left, right) => left.order - right.order)

        return rootMembers.length > 0
          ? `${rootMembers[0].agentName ?? rootMembers[0].agentId}${rootMembers.length > 1 ? ` +${rootMembers.length - 1}` : ""}`
          : "-"
      },
    },
    {
      id: "actions",
      enableSorting: false,
      enableHiding: false,
      header: () => (
        <div className="text-right">{t("agentTeams.table.actions")}</div>
      ),
      cell: ({ row }) => (
        <div className="text-right">
          <Button asChild size="sm" variant="outline">
            <Link to="/agent-teams/$teamId/edit" params={{ teamId: row.original.id }}>
              <SquarePenIcon data-icon="inline-start" />
              {t("identity.actions.edit")}
            </Link>
          </Button>
        </div>
      ),
    },
  ]
}
