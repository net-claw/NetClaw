import { Link } from "@tanstack/react-router"
import type { ColumnDef, SortingState } from "@tanstack/react-table"
import {
  ArrowDownAZIcon,
  ArrowUpAZIcon,
  ChevronsUpDownIcon,
  SquarePenIcon,
} from "lucide-react"

import type { AgentModel } from "@/@types/models"
import { Button } from "@/components/ui/button"
import { Checkbox } from "@/components/ui/checkbox"
import type { TFunction } from "i18next"

type AgentColumnsOptions = {
  t: TFunction
  sorting: SortingState
}

export function getAgentColumns({
  t,
  sorting,
}: AgentColumnsOptions): ColumnDef<AgentModel>[] {
  const nameSorted = sorting[0]?.id === "name" ? sorting[0] : null

  return [
    {
      id: "select",
      enableSorting: false,
      enableHiding: false,
      header: ({ table }) => (
        <Checkbox
          aria-label={t("agents.table.selectAll")}
          checked={table.getIsAllPageRowsSelected()}
          onCheckedChange={(checked) =>
            table.toggleAllPageRowsSelected(checked === true)
          }
        />
      ),
      cell: ({ row }) => (
        <Checkbox
          aria-label={t("agents.table.selectRow", {
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
          {t("agents.table.name")}
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
          <div className="text-xs text-muted-foreground">{row.original.status}</div>
        </div>
      ),
    },
    {
      accessorKey: "role",
      header: () => t("agents.table.role"),
    },
    {
      accessorKey: "kind",
      header: () => t("agents.table.kind"),
    },
    {
      accessorKey: "type",
      header: () => t("agents.table.type"),
    },
    {
      id: "providers",
      header: () => t("agents.table.provider"),
      cell: ({ row }) =>
        row.original.providers.length > 0
          ? `${row.original.providers[0].name}${row.original.providers.length > 1 ? ` +${row.original.providers.length - 1}` : ""}`
          : "-",
    },
    {
      id: "actions",
      enableSorting: false,
      enableHiding: false,
      header: () => <div className="text-right">{t("agents.table.actions")}</div>,
      cell: ({ row }) => (
        <div className="text-right">
          <Button asChild size="sm" variant="outline">
            <Link to="/agents/$agentId/edit" params={{ agentId: row.original.id }}>
              <SquarePenIcon data-icon="inline-start" />
              {t("identity.actions.edit")}
            </Link>
          </Button>
        </div>
      ),
    },
  ]
}
