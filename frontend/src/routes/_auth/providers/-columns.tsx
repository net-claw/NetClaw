import { Link } from "@tanstack/react-router"
import type { ColumnDef, SortingState } from "@tanstack/react-table"
import {
  ArrowDownAZIcon,
  ArrowUpAZIcon,
  ChevronsUpDownIcon,
  SquarePenIcon,
} from "lucide-react"

import type { ProviderModel } from "@/@types/models"
import { Button } from "@/components/ui/button"
import { Checkbox } from "@/components/ui/checkbox"
import type { TFunction } from "i18next"
import { providerData } from "@/constants/data"

type ProviderColumnsOptions = {
  t: TFunction
  sorting: SortingState
}

export function getProviderColumns({
  t,
  sorting,
}: ProviderColumnsOptions): ColumnDef<ProviderModel>[] {
  const nameSorted = sorting[0]?.id === "name" ? sorting[0] : null
  const updatedAtSorted = sorting[0]?.id === "updatedAt" ? sorting[0] : null

  return [
    {
      id: "select",
      enableSorting: false,
      enableHiding: false,
      header: ({ table }) => (
        <Checkbox
          aria-label={t("providers.table.selectAll")}
          checked={table.getIsAllPageRowsSelected()}
          onCheckedChange={(checked) =>
            table.toggleAllPageRowsSelected(checked === true)
          }
        />
      ),
      cell: ({ row }) => (
        <Checkbox
          aria-label={t("providers.table.selectRow", {
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
          {t("providers.table.name")}
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
      accessorKey: "providerType",
      header: () => t("providers.table.provider"),
      cell: ({ row }) => {
        const providerType = row.original.providerType
        const imageSrc = providerData.find(
          (p) => p.value === providerType
        )?.image
        return (
          <div className="flex items-center gap-x-2">
            {imageSrc && (
              <img
                src={imageSrc}
                alt={providerType}
                className="h-6 w-6 rounded-md"
              />
            )}

            <span className="font-medium">{providerType}</span>
          </div>
        )
      },
    },
    {
      accessorKey: "defaultModel",
      header: () => t("providers.table.model"),
    },
    {
      accessorKey: "isActive",
      header: () => t("providers.table.status"),
      cell: ({ row }) => (
        <div>
          {row.original.isActive
            ? t("providers.status.active")
            : t("providers.status.inactive")}
        </div>
      ),
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
          {t("providers.table.updatedAt")}
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
        <div className="text-right">{t("providers.table.actions")}</div>
      ),
      cell: ({ row }) => (
        <div className="text-right">
          <Button asChild size="sm" variant="outline">
            <Link
              to="/providers/$providerId/edit"
              params={{ providerId: row.original.id }}
            >
              <SquarePenIcon data-icon="inline-start" />
              {t("identity.actions.edit")}
            </Link>
          </Button>
        </div>
      ),
    },
  ]
}
