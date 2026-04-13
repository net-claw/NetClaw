import { useNavigate } from "@tanstack/react-router"

import {
  updateSkillSchema,
  type UpdateSkillModel,
} from "@/@types/models"
import { PageHeaderCard } from "@/components/share/cards/page-header-card"
import { SectionCard } from "@/components/share/cards/section-card"
import { Button } from "@/components/ui/button"
import {
  useGetSkillById,
  useInstallSkill,
  useUpdateSkill,
} from "@/hooks/api/use-skill"
import { actionIcons, appIcons } from "@/lib/icons"
import {
  formatSkillInstallLabel,
  parseSkillInstallState,
} from "@/lib/skill-install"
import { SkillForm } from "@/pages/auth/skills/skill-form"
import { useTranslation } from "react-i18next"

type SkillsEditPageProps = {
  skillId: string
}

export default function SkillsEditPage({ skillId }: SkillsEditPageProps) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const SkillsIcon = appIcons.settings
  const BackIcon = actionIcons.back
  const InstallIcon = actionIcons.upload
  const { data: skill, isLoading } = useGetSkillById(skillId)
  const updateSkillMutation = useUpdateSkill()
  const installSkillMutation = useInstallSkill()

  const handleSubmit = async (values: UpdateSkillModel) => {
    await updateSkillMutation.mutateAsync({
      skillId,
      payload: values,
    })
  }

  return (
    <>
      <PageHeaderCard
        icon={<SkillsIcon />}
        title={t("skills.editTitle")}
        description={t("skills.editDescription")}
        headerRight={
          <Button
            type="button"
            variant="outline"
            onClick={() => navigate({ to: "/llm/skills" })}
          >
            <BackIcon data-icon="inline-start" />
            {t("skills.actions.backToSkills")}
          </Button>
        }
      />

      <SectionCard contentClassName="flex flex-col gap-6">
        {isLoading || !skill ? (
          <p className="text-sm text-muted-foreground">
            {t("skills.errors.loadSkill")}
          </p>
        ) : (
          <>
            {(() => {
              const installState = parseSkillInstallState(skill.metadata_json)
              const canInstall =
                installState?.status === "missing" ||
                installState?.status === "failed"

              return (
                <div className="rounded-lg border p-4">
                  <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
                    <div className="space-y-1">
                      <div className="text-sm font-medium">
                        Runtime install: {formatSkillInstallLabel(installState)}
                      </div>
                      {installState?.missingCommands?.length ? (
                        <div className="text-xs text-muted-foreground">
                          Missing commands:{" "}
                          {installState.missingCommands.join(", ")}
                        </div>
                      ) : null}
                      {installState?.installedAt ? (
                        <div className="text-xs text-muted-foreground">
                          Installed at: {installState.installedAt}
                        </div>
                      ) : null}
                      {installState?.lastError ? (
                        <div className="text-xs text-destructive">
                          {installState.lastError}
                        </div>
                      ) : null}
                    </div>

                    {canInstall ? (
                      <Button
                        type="button"
                        variant="outline"
                        disabled={installSkillMutation.isPending}
                        onClick={() => installSkillMutation.mutate(skill.id)}
                      >
                        <InstallIcon data-icon="inline-start" />
                        Install skill
                      </Button>
                    ) : null}
                  </div>
                </div>
              )
            })()}

            <SkillForm
              schema={updateSkillSchema}
              pending={updateSkillMutation.isPending}
              initialValues={{
                name: skill.name,
                slug: skill.slug,
                description: skill.description,
                file_name: skill.file_name,
                content: skill.content,
                status: skill.status as UpdateSkillModel["status"],
                metadata_json: skill.metadata_json ?? "",
              }}
              onSubmit={handleSubmit}
            />
          </>
        )}
      </SectionCard>
    </>
  )
}
