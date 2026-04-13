import { useNavigate } from "@tanstack/react-router"

import { createAgentSchema, type CreateAgentModel } from "@/@types/models"
import { PageHeaderCard } from "@/components/share/cards/page-header-card"
import { SectionCard } from "@/components/share/cards/section-card"
import { Button } from "@/components/ui/button"
import { useCreateAgent } from "@/hooks/api/use-agent"
import { useGetProviderList } from "@/hooks/api/use-provider"
import { useGetSkillList } from "@/hooks/api/use-skill"
import { actionIcons, appIcons } from "@/lib/icons"
import { AgentForm } from "@/pages/auth/agents/agent-form"
import { useTranslation } from "react-i18next"

export default function AgentsCreatePage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const createAgentMutation = useCreateAgent()
  const AgentsIcon = appIcons.agents
  const BackIcon = actionIcons.back
  const { data: providersData } = useGetProviderList({
    pageIndex: 0,
    pageSize: 100,
    ascending: true,
  })
  const { data: skillsData } = useGetSkillList({
    pageIndex: 0,
    pageSize: 100,
    ascending: true,
    status: "active",
  })

  const providers = providersData?.items ?? []
  const skills = skillsData?.items ?? []

  const handleSubmit = async (values: CreateAgentModel) => {
    const agent = await createAgentMutation.mutateAsync(values)
    navigate({ to: "/agents/$agentId/edit", params: { agentId: agent.id } })
  }

  return (
    <>
      <PageHeaderCard
        icon={<AgentsIcon />}
        title={t("agents.createTitle")}
        description={t("agents.createDescription")}
        headerRight={
          <Button
            type="button"
            variant="outline"
            onClick={() => navigate({ to: "/agents" })}
          >
            <BackIcon data-icon="inline-start" />
            {t("agents.actions.backToAgents")}
          </Button>
        }
      />

      <SectionCard contentClassName="flex flex-col gap-6">
        <p className="text-sm text-muted-foreground">
          {t("agents.createHelper")}
        </p>

        <AgentForm
          schema={createAgentSchema}
          providers={providers}
          skills={skills}
          pending={createAgentMutation.isPending}
          onSubmit={handleSubmit}
        />
      </SectionCard>
    </>
  )
}
