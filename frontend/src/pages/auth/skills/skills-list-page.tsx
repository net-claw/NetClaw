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
import { useSkillsTable } from "@/routes/_auth/skills/-use-skills-table"
import { useTranslation } from "react-i18next"

export default function SkillsListPage() {
  const { t } = useTranslation()
  const [query, setQuery] = useState("")
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false)
  const SkillsIcon = appIcons.settings
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
    selectedSkillIds,
    handleDeleteSelected,
    deleteSkillsMutation,
    pageSizeOptions,
    setPagination,
  } = useSkillsTable(query)

  return (
    <>
      <PageHeaderCard
        icon={<SkillsIcon />}
        title={t("skills.page.title")}
        description={t("skills.page.description")}
        titleMeta={totalItems}
        headerRight={
          <Button asChild size="lg">
            <Link to="/llm/skills/create">
              <CreateIcon data-icon="inline-start" />
              {t("skills.actions.newSkill")}
            </Link>
          </Button>
        }
      />

      <SectionCard
        headerRight={
          selectedSkillIds.length > 0 ? (
            <DeleteConfirmDialog
              open={isDeleteDialogOpen}
              onOpenChange={setIsDeleteDialogOpen}
              title={t("skills.deleteDialog.title")}
              description={t("skills.deleteDialog.description", {
                count: selectedSkillIds.length,
              })}
              confirmLabel={t("skills.deleteDialog.confirm")}
              cancelLabel={t("skills.deleteDialog.cancel")}
              onConfirm={async () => {
                await handleDeleteSelected()
                setIsDeleteDialogOpen(false)
              }}
              trigger={
                <Button
                  type="button"
                  size="sm"
                  variant="destructive"
                  disabled={deleteSkillsMutation.isPending}
                >
                  <DeleteIcon data-icon="inline-start" />
                  {t("skills.actions.deleteSelected", {
                    count: selectedSkillIds.length,
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
                  placeholder={t("skills.searchPlaceholder")}
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
              {t("skills.errors.loadSkills")}
            </div>
          ) : (
            <>
              <DataTable
                table={table}
                loading={isLoading}
                emptyMessage={t("skills.emptySkills")}
              />
              <DataTablePagination
                table={table}
                rowCount={totalItems}
                isFetching={isFetching && !isLoading}
                pageSizeOptions={pageSizeOptions}
                selectedCount={selectedSkillIds.length}
              />
            </>
          )}
        </div>
      </SectionCard>
    </>
  )
}
