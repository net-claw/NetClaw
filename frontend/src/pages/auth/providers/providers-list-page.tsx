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
import { useDeleteProviders, useGetProviderList } from "@/hooks/api/use-provider"
import { actionIcons, appIcons } from "@/lib/icons"
import { useTranslation } from "react-i18next"

export default function ProvidersListPage() {
  const { t } = useTranslation()
  const [query, setQuery] = useState("")
  const [selectedIds, setSelectedIds] = useState<string[]>([])
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false)
  const ProvidersIcon = appIcons.providers
  const CreateIcon = actionIcons.create
  const DeleteIcon = actionIcons.delete
  const SearchIcon = actionIcons.search
  const RefreshIcon = actionIcons.refresh
  const deleteProvidersMutation = useDeleteProviders()

  const { data, isLoading, isFetching, refetch } = useGetProviderList({
    pageIndex: 0,
    pageSize: 50,
    searchText: query.trim() || undefined,
    orderBy: "updatedAt",
    ascending: false,
  })

  const providers = data?.items ?? []
  const totalItems = data?.totalItems ?? 0
  const selectedIdSet = useMemo(() => new Set(selectedIds), [selectedIds])

  return (
    <>
      <PageHeaderCard
        icon={<ProvidersIcon />}
        title={t("providers.page.title")}
        description={t("providers.page.description")}
        titleMeta={totalItems}
        headerRight={
          <Button asChild size="lg">
            <Link to="/providers/create">
              <CreateIcon data-icon="inline-start" />
              {t("providers.actions.newProvider")}
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
              title={t("providers.deleteDialog.title")}
              description={t("providers.deleteDialog.description_other", {
                count: selectedIds.length,
              })}
              confirmLabel={t("providers.deleteDialog.confirm")}
              cancelLabel={t("providers.deleteDialog.cancel")}
              onConfirm={async () => {
                await deleteProvidersMutation.mutateAsync(selectedIds)
                setSelectedIds([])
                setIsDeleteDialogOpen(false)
              }}
              trigger={
                <Button type="button" size="sm" variant="destructive">
                  <DeleteIcon data-icon="inline-start" />
                  {t("providers.actions.deleteSelected_other", {
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
                  placeholder={t("providers.searchPlaceholder")}
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
                <TableHead>{t("providers.table.name")}</TableHead>
                <TableHead>{t("providers.table.provider")}</TableHead>
                <TableHead>{t("providers.table.model")}</TableHead>
                <TableHead>{t("providers.table.status")}</TableHead>
                <TableHead>{t("providers.table.updatedAt")}</TableHead>
                <TableHead>{t("providers.table.actions")}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {providers.map((provider) => (
                <TableRow key={provider.id}>
                  <TableCell>
                    <input
                      type="checkbox"
                      checked={selectedIdSet.has(provider.id)}
                      onChange={(event) => {
                        setSelectedIds((current) =>
                          event.target.checked
                            ? [...current, provider.id]
                            : current.filter((id) => id !== provider.id)
                        )
                      }}
                    />
                  </TableCell>
                  <TableCell className="font-medium">{provider.name}</TableCell>
                  <TableCell>{provider.providerType}</TableCell>
                  <TableCell>{provider.defaultModel}</TableCell>
                  <TableCell>
                    {provider.isActive ? "active" : "inactive"}
                  </TableCell>
                  <TableCell>
                    {provider.updatedOn ?? provider.createdOn}
                  </TableCell>
                  <TableCell>
                    <Button asChild size="sm" variant="outline">
                      <Link
                        to="/providers/$providerId/edit"
                        params={{ providerId: provider.id }}
                      >
                        {t("identity.actions.edit")}
                      </Link>
                    </Button>
                  </TableCell>
                </TableRow>
              ))}

              {!isLoading && providers.length === 0 ? (
                <TableRow>
                  <TableCell
                    colSpan={7}
                    className="text-center text-sm text-muted-foreground"
                  >
                    {t("providers.emptyProviders")}
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
