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
import {
  useDeleteSkills,
  useGetSkillList,
  useInstallSkill,
} from "@/hooks/api/use-skill"
import { actionIcons, appIcons } from "@/lib/icons"
import {
  formatSkillInstallLabel,
  parseSkillInstallState,
} from "@/lib/skill-install"
import { useTranslation } from "react-i18next"

export default function SkillsListPage() {
  const { t } = useTranslation()
  const [query, setQuery] = useState("")
  const [selectedIds, setSelectedIds] = useState<string[]>([])
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false)
  const SkillsIcon = appIcons.settings
  const CreateIcon = actionIcons.create
  const DeleteIcon = actionIcons.delete
  const SearchIcon = actionIcons.search
  const RefreshIcon = actionIcons.refresh
  const InstallIcon = actionIcons.upload
  const deleteSkillsMutation = useDeleteSkills()
  const installSkillMutation = useInstallSkill()

  const { data, isLoading, isFetching, refetch } = useGetSkillList({
    pageIndex: 0,
    pageSize: 50,
    searchText: query.trim() || undefined,
    orderBy: "updated_at",
    ascending: false,
  })

  const skills = data?.items ?? []
  const totalItems = data?.totalItems ?? 0
  const selectedIdSet = useMemo(() => new Set(selectedIds), [selectedIds])

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
          selectedIds.length > 0 ? (
            <DeleteConfirmDialog
              open={isDeleteDialogOpen}
              onOpenChange={setIsDeleteDialogOpen}
              title={t("skills.deleteDialog.title")}
              description={t("skills.deleteDialog.description", {
                count: selectedIds.length,
              })}
              confirmLabel={t("skills.deleteDialog.confirm")}
              cancelLabel={t("skills.deleteDialog.cancel")}
              onConfirm={async () => {
                await deleteSkillsMutation.mutateAsync(selectedIds)
                setSelectedIds([])
                setIsDeleteDialogOpen(false)
              }}
              trigger={
                <Button type="button" size="sm" variant="destructive">
                  <DeleteIcon data-icon="inline-start" />
                  {t("skills.actions.deleteSelected", {
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
                  placeholder={t("skills.searchPlaceholder")}
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
                <TableHead>{t("skills.table.name")}</TableHead>
                <TableHead>{t("skills.table.fileName")}</TableHead>
                <TableHead>{t("skills.table.status")}</TableHead>
                <TableHead>Install</TableHead>
                <TableHead>{t("skills.table.updatedAt")}</TableHead>
                <TableHead>{t("skills.table.actions")}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {skills.map((skill) => {
                const installState = parseSkillInstallState(skill.metadata_json)
                const canInstall =
                  installState?.status === "missing" ||
                  installState?.status === "failed"

                return (
                  <TableRow key={skill.id}>
                    <TableCell>
                      <input
                        type="checkbox"
                        checked={selectedIdSet.has(skill.id)}
                        onChange={(event) => {
                          setSelectedIds((current) =>
                            event.target.checked
                              ? [...current, skill.id]
                              : current.filter((id) => id !== skill.id)
                          )
                        }}
                      />
                    </TableCell>
                    <TableCell className="font-medium">
                      <div>{skill.name}</div>
                      <div className="text-xs text-muted-foreground">
                        {skill.slug}
                      </div>
                    </TableCell>
                    <TableCell>{skill.file_name}</TableCell>
                    <TableCell>{skill.status}</TableCell>
                    <TableCell>
                      <div className="space-y-1">
                        <div className="text-sm">
                          {formatSkillInstallLabel(installState)}
                        </div>
                        {installState?.missingCommands?.length ? (
                          <div className="text-xs text-muted-foreground">
                            Missing: {installState.missingCommands.join(", ")}
                          </div>
                        ) : null}
                        {installState?.lastError ? (
                          <div className="text-xs text-destructive">
                            {installState.lastError}
                          </div>
                        ) : null}
                        {canInstall ? (
                          <Button
                            type="button"
                            size="sm"
                            variant="outline"
                            disabled={installSkillMutation.isPending}
                            onClick={() =>
                              installSkillMutation.mutate(skill.id)
                            }
                          >
                            <InstallIcon data-icon="inline-start" />
                            Install
                          </Button>
                        ) : null}
                      </div>
                    </TableCell>
                    <TableCell>{skill.updated_at ?? skill.created_at}</TableCell>
                    <TableCell>
                      <Button asChild size="sm" variant="outline">
                        <Link
                          to="/llm/skills/$skillId/edit"
                          params={{ skillId: skill.id }}
                        >
                          {t("identity.actions.edit")}
                        </Link>
                      </Button>
                    </TableCell>
                  </TableRow>
                )
              })}

              {!isLoading && skills.length === 0 ? (
                <TableRow>
                  <TableCell
                    colSpan={7}
                    className="text-center text-sm text-muted-foreground"
                  >
                    {t("skills.emptySkills")}
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
