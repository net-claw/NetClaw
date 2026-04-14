import { Link } from "@tanstack/react-router"
import { useState } from "react"
import { useTranslation } from "react-i18next"

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
import { useChannelsTable } from "@/routes/_auth/channels/-use-channels-table"

export default function ChannelsListPage() {
  const { t } = useTranslation()
  const [query, setQuery] = useState("")
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false)
  const ChannelsIcon = appIcons.channels
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
    selectedChannelIds,
    handleDeleteSelected,
    deleteChannelsMutation,
    pageSizeOptions,
    setPagination,
  } = useChannelsTable(query)

  return (
    <>
      <PageHeaderCard
        icon={<ChannelsIcon />}
        title={t("channels.page.title")}
        description={t("channels.page.description")}
        titleMeta={totalItems}
        headerRight={
          <Button asChild size="lg">
            <Link to="/channels/create">
              <CreateIcon data-icon="inline-start" />
              {t("channels.actions.newChannel")}
            </Link>
          </Button>
        }
      />

      <SectionCard
        headerRight={
          selectedChannelIds.length > 0 ? (
            <DeleteConfirmDialog
              open={isDeleteDialogOpen}
              onOpenChange={setIsDeleteDialogOpen}
              title={t("channels.deleteDialog.title")}
              description={t("channels.deleteDialog.description_other", {
                count: selectedChannelIds.length,
              })}
              confirmLabel={t("channels.deleteDialog.confirm")}
              cancelLabel={t("channels.deleteDialog.cancel")}
              onConfirm={async () => {
                await handleDeleteSelected()
                setIsDeleteDialogOpen(false)
              }}
              trigger={
                <Button
                  type="button"
                  size="sm"
                  variant="destructive"
                  disabled={deleteChannelsMutation.isPending}
                >
                  <DeleteIcon data-icon="inline-start" />
                  {t("channels.actions.deleteSelected_other", {
                    count: selectedChannelIds.length,
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
                  placeholder={t("channels.searchPlaceholder")}
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
              {t("channels.errors.loadChannels")}
            </div>
          ) : (
            <>
              <DataTable
                table={table}
                loading={isLoading}
                emptyMessage={t("channels.emptyChannels")}
              />
              <DataTablePagination
                table={table}
                rowCount={totalItems}
                isFetching={isFetching && !isLoading}
                pageSizeOptions={pageSizeOptions}
                selectedCount={selectedChannelIds.length}
              />
            </>
          )}
        </div>
      </SectionCard>
    </>
  )
}
