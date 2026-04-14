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
import { useProvidersTable } from "@/routes/_auth/providers/-use-providers-table"
import { useTranslation } from "react-i18next"

export default function ProvidersListPage() {
  const { t } = useTranslation()
  const [query, setQuery] = useState("")
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false)
  const ProvidersIcon = appIcons.providers
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
    selectedProviderIds,
    handleDeleteSelected,
    deleteProvidersMutation,
    pageSizeOptions,
    setPagination,
  } = useProvidersTable(query)

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
          selectedProviderIds.length > 0 ? (
            <DeleteConfirmDialog
              open={isDeleteDialogOpen}
              onOpenChange={setIsDeleteDialogOpen}
              title={t("providers.deleteDialog.title")}
              description={t("providers.deleteDialog.description_other", {
                count: selectedProviderIds.length,
              })}
              confirmLabel={t("providers.deleteDialog.confirm")}
              cancelLabel={t("providers.deleteDialog.cancel")}
              onConfirm={async () => {
                await handleDeleteSelected()
                setIsDeleteDialogOpen(false)
              }}
              trigger={
                <Button
                  type="button"
                  size="sm"
                  variant="destructive"
                  disabled={deleteProvidersMutation.isPending}
                >
                  <DeleteIcon data-icon="inline-start" />
                  {t("providers.actions.deleteSelected_other", {
                    count: selectedProviderIds.length,
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
              {t("providers.errors.loadProviders")}
            </div>
          ) : (
            <>
              <DataTable
                table={table}
                loading={isLoading}
                emptyMessage={t("providers.emptyProviders")}
              />
              <DataTablePagination
                table={table}
                rowCount={totalItems}
                isFetching={isFetching && !isLoading}
                pageSizeOptions={pageSizeOptions}
                selectedCount={selectedProviderIds.length}
              />
            </>
          )}
        </div>
      </SectionCard>
    </>
  )
}
