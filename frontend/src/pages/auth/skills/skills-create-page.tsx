import { useNavigate } from "@tanstack/react-router"
import { useRef } from "react"

import {
  createSkillSchema,
  type CreateSkillModel,
} from "@/@types/models"
import { PageHeaderCard } from "@/components/share/cards/page-header-card"
import { SectionCard } from "@/components/share/cards/section-card"
import { Button } from "@/components/ui/button"
import { useCreateSkill, useUploadSkill } from "@/hooks/api/use-skill"
import { actionIcons, appIcons } from "@/lib/icons"
import { SkillForm } from "@/pages/auth/skills/skill-form"
import { useTranslation } from "react-i18next"

export default function SkillsCreatePage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const createSkillMutation = useCreateSkill()
  const uploadSkillMutation = useUploadSkill()
  const fileInputRef = useRef<HTMLInputElement | null>(null)
  const SkillsIcon = appIcons.settings
  const BackIcon = actionIcons.back
  const UploadIcon = actionIcons.upload

  const handleSubmit = async (values: CreateSkillModel) => {
    const skill = await createSkillMutation.mutateAsync(values)
    navigate({ to: "/llm/skills/$skillId/edit", params: { skillId: skill.id } })
  }

  return (
    <>
      <PageHeaderCard
        icon={<SkillsIcon />}
        title={t("skills.createTitle")}
        description={t("skills.createDescription")}
        headerRight={
          <div className="flex gap-2">
            <input
              ref={fileInputRef}
              type="file"
              accept=".zip,.md"
              className="hidden"
              onChange={async (event) => {
                const file = event.target.files?.[0]
                if (!file) {
                  return
                }

                const skill = await uploadSkillMutation.mutateAsync(file)
                navigate({
                  to: "/llm/skills/$skillId/edit",
                  params: { skillId: skill.id },
                })
              }}
            />
            <Button
              type="button"
              variant="outline"
              onClick={() => fileInputRef.current?.click()}
              disabled={uploadSkillMutation.isPending}
            >
              <UploadIcon data-icon="inline-start" />
              {t("skills.actions.uploadArchive")}
            </Button>
            <Button
              type="button"
              variant="outline"
              onClick={() => navigate({ to: "/llm/skills" })}
            >
              <BackIcon data-icon="inline-start" />
              {t("skills.actions.backToSkills")}
            </Button>
          </div>
        }
      />

      <SectionCard contentClassName="flex flex-col gap-6">
        <p className="text-sm text-muted-foreground">
          {t("skills.createHelper")}
        </p>

        <SkillForm
          schema={createSkillSchema}
          pending={createSkillMutation.isPending}
          onSubmit={handleSubmit}
        />
      </SectionCard>
    </>
  )
}
