import { Link } from "@tanstack/react-router"
import type { ColumnDef, SortingState } from "@tanstack/react-table"
import {
  ArrowDownAZIcon,
  ArrowUpAZIcon,
  ChevronsUpDownIcon,
  SquarePenIcon,
} from "lucide-react"

import type { ChannelModel } from "@/@types/models"
import { Button } from "@/components/ui/button"
import { Checkbox } from "@/components/ui/checkbox"
import { channelData } from "@/constants/data"
import type { TFunction } from "i18next"

type ChannelColumnsOptions = {
  t: TFunction
  sorting: SortingState
  startPending: boolean
  stopPending: boolean
  restartPending: boolean
  onStart: (channelId: string) => void
  onStop: (channelId: string) => void
  onRestart: (channelId: string) => void
}

export function getChannelColumns({
  t,
  sorting,
  startPending,
  stopPending,
  restartPending,
  onStart,
  onStop,
  onRestart,
}: ChannelColumnsOptions): ColumnDef<ChannelModel>[] {
  const nameSorted = sorting[0]?.id === "name" ? sorting[0] : null
  const updatedAtSorted = sorting[0]?.id === "updatedAt" ? sorting[0] : null

  return [
    {
      id: "select",
      enableSorting: false,
      enableHiding: false,
      header: ({ table }) => (
        <Checkbox
          aria-label={t("channels.table.selectAll")}
          checked={table.getIsAllPageRowsSelected()}
          onCheckedChange={(checked) =>
            table.toggleAllPageRowsSelected(checked === true)
          }
        />
      ),
      cell: ({ row }) => (
        <Checkbox
          aria-label={t("channels.table.selectRow", {
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
          className="-ml-2 h-8 px-2 text-xs font-medium tracking-wide text-muted-foreground uppercase hover:bg-transparent hover:text-foreground"
          onClick={() => column.toggleSorting(nameSorted?.desc === false)}
        >
          {t("channels.table.name")}
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
        <span className="font-medium">{row.original.name}</span>
      ),
    },
    {
      accessorKey: "kind",
      header: () => t("channels.table.kind"),
      cell: ({ row }) => (
        <div className="flex items-center gap-x-2">
          <img
            src={
              channelData.find((item) => item.value === row.original.kind)
                ?.image
            }
            alt={row.original.kind}
            className="h-8 w-8 rounded-lg"
          />
          <span>{row.original.kind}</span>
        </div>
      ),
    },
    {
      accessorKey: "status",
      header: () => t("channels.table.status"),
      cell: ({ row }) => {
        const status = row.original.status
        const dotColor: Record<string, string> = {
          running: "bg-green-500",
          starting: "bg-yellow-400 animate-pulse",
          stopping: "bg-yellow-400 animate-pulse",
          stopped: "bg-zinc-400",
          error: "bg-red-500",
        }
        return (
          <div className="flex items-center gap-x-2">
            <span className={`h-2 w-2 rounded-full ${dotColor[status] ?? "bg-zinc-400"}`} />
            <span>{t(`channels.statuses.${status}`, { defaultValue: status })}</span>
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
          className="-ml-2 h-8 px-2 text-xs font-medium tracking-wide text-muted-foreground uppercase hover:bg-transparent hover:text-foreground"
          onClick={() => column.toggleSorting(updatedAtSorted?.desc === false)}
        >
          {t("channels.table.updatedAt")}
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
      cell: ({ row }) => row.original.updatedOn ?? row.original.createdOn,
    },
    {
      id: "actions",
      enableSorting: false,
      enableHiding: false,
      header: () => (
        <div className="text-right">{t("channels.table.actions")}</div>
      ),
      cell: ({ row }) => {
        const status = row.original.status
        const isTransitioning = status === "starting" || status === "stopping"
        const canStart = status === "stopped" || status === "error"
        const canStop = status === "running"
        const canRestart = status === "running" || status === "error"
        return (
          <div className="flex justify-end">
            <div className="flex flex-wrap justify-end gap-2">
              <Button asChild size="sm" variant="outline">
                <Link
                  to="/channels/$channelId/edit"
                  params={{ channelId: row.original.id }}
                >
                  <SquarePenIcon data-icon="inline-start" />
                  {t("identity.actions.edit")}
                </Link>
              </Button>
              {canStart && (
                <Button
                  type="button"
                  size="sm"
                  variant="outline"
                  disabled={startPending || isTransitioning}
                  onClick={() => onStart(row.original.id)}
                >
                  {t("channels.actions.start")}
                </Button>
              )}
              {canStop && (
                <Button
                  type="button"
                  size="sm"
                  variant="outline"
                  disabled={stopPending || isTransitioning}
                  onClick={() => onStop(row.original.id)}
                >
                  {t("channels.actions.stop")}
                </Button>
              )}
              {canRestart && (
                <Button
                  type="button"
                  size="sm"
                  variant="outline"
                  disabled={restartPending || isTransitioning}
                  onClick={() => onRestart(row.original.id)}
                >
                  {t("channels.actions.restart")}
                </Button>
              )}
            </div>
          </div>
        )
      },
    },
  ]
}
