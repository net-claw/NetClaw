import { Link } from "@tanstack/react-router"
import type { ColumnDef, SortingState } from "@tanstack/react-table"
import {
  ArrowDownAZIcon,
  ArrowUpAZIcon,
  ChevronsUpDownIcon,
  SquarePenIcon,
} from "lucide-react"

import type { SkillModel } from "@/@types/models"
import { Button } from "@/components/ui/button"
import { Checkbox } from "@/components/ui/checkbox"
import {
  formatSkillInstallLabel,
  parseSkillInstallState,
} from "@/lib/skill-install"
import type { TFunction } from "i18next"

type SkillColumnsOptions = {
  t: TFunction
  sorting: SortingState
  installPending: boolean
  onInstall: (skillId: string) => void
}

export function getSkillColumns({
  t,
  sorting,
  installPending,
  onInstall,
}: SkillColumnsOptions): ColumnDef<SkillModel>[] {
  const nameSorted = sorting[0]?.id === "name" ? sorting[0] : null
  const updatedAtSorted = sorting[0]?.id === "updatedAt" ? sorting[0] : null

  return [
    {
      id: "select",
      enableSorting: false,
      enableHiding: false,
      header: ({ table }) => (
        <Checkbox
          aria-label={t("skills.table.selectAll")}
          checked={table.getIsAllPageRowsSelected()}
          onCheckedChange={(checked) =>
            table.toggleAllPageRowsSelected(checked === true)
          }
        />
      ),
      cell: ({ row }) => (
        <Checkbox
          aria-label={t("skills.table.selectRow", {
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
          {t("skills.table.name")}
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
          <div className="text-xs text-muted-foreground">{row.original.slug}</div>
        </div>
      ),
    },
    {
      accessorKey: "fileName",
      header: () => t("skills.table.fileName"),
    },
    {
      accessorKey: "status",
      header: () => t("skills.table.status"),
    },
    {
      id: "install",
      header: () => t("skills.table.install"),
      cell: ({ row }) => {
        const installState = parseSkillInstallState(row.original.metadataJson)
        const canInstall =
          installState?.status === "missing" || installState?.status === "failed"

        return (
          <div className="space-y-1">
            <div className="text-sm">{formatSkillInstallLabel(installState)}</div>
            {installState?.missingCommands?.length ? (
              <div className="text-xs text-muted-foreground">
                {t("skills.table.missingCommands", {
                  commands: installState.missingCommands.join(", "),
                })}
              </div>
            ) : null}
            {installState?.lastError ? (
              <div className="text-xs text-destructive">{installState.lastError}</div>
            ) : null}
            {canInstall ? (
              <Button
                type="button"
                size="sm"
                variant="outline"
                disabled={installPending}
                onClick={() => onInstall(row.original.id)}
              >
                {t("skills.actions.install")}
              </Button>
            ) : null}
          </div>
        )
      },
    },
    {
      id: "updatedAt",
      header: ({ column }) => (
        <Button
          type="button"
          variant="ghost"
          size="sm"
          className="-ml-2 h-8 px-2 text-xs font-medium uppercase tracking-wide text-muted-foreground hover:bg-transparent hover:text-foreground"
          onClick={() => column.toggleSorting(updatedAtSorted?.desc === false)}
        >
          {t("skills.table.updatedAt")}
          {updatedAtSorted ? (
            updatedAtSorted.desc ? (
              <ArrowDownAZIcon data-icon="inline-end" />
            ) : (
              <ArrowUpAZIcon data-icon="inline-end" />
            )
          ) : (
            <ChevronsUpDownIcon data-icon="inline-end" />
          )}
        </Button>
      ),
      cell: ({ row }) => row.original.updatedAt ?? row.original.createdAt,
    },
    {
      id: "actions",
      enableSorting: false,
      enableHiding: false,
      header: () => <div className="text-right">{t("skills.table.actions")}</div>,
      cell: ({ row }) => (
        <div className="text-right">
          <Button asChild size="sm" variant="outline">
            <Link to="/llm/skills/$skillId/edit" params={{ skillId: row.original.id }}>
              <SquarePenIcon data-icon="inline-start" />
              {t("identity.actions.edit")}
            </Link>
          </Button>
        </div>
      ),
    },
  ]
}
