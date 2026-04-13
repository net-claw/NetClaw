import { useNavigate } from "@tanstack/react-router"

import { updateAgentSchema, type UpdateAgentModel } from "@/@types/models"
import { PageHeaderCard } from "@/components/share/cards/page-header-card"
import { SectionCard } from "@/components/share/cards/section-card"
import { Button } from "@/components/ui/button"
import { useGetAgentById, useUpdateAgent } from "@/hooks/api/use-agent"
import { useGetProviderList } from "@/hooks/api/use-provider"
import { useGetSkillList } from "@/hooks/api/use-skill"
import { actionIcons, appIcons } from "@/lib/icons"
import { AgentForm } from "@/pages/auth/agents/agent-form"
import { useTranslation } from "react-i18next"

type AgentsEditPageProps = {
  agentId: string
}

export default function AgentsEditPage({ agentId }: AgentsEditPageProps) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const AgentsIcon = appIcons.agents
  const BackIcon = actionIcons.back
  const { data: agent, isLoading } = useGetAgentById(agentId)
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
  const updateAgentMutation = useUpdateAgent()
  const providers = providersData?.items ?? []
  const skills = skillsData?.items ?? []

  const handleSubmit = async (values: UpdateAgentModel) => {
    await updateAgentMutation.mutateAsync({
      agentId,
      payload: values,
    })
  }

  return (
    <>
      <PageHeaderCard
        icon={<AgentsIcon />}
        title={t("agents.editTitle")}
        description={t("agents.editDescription")}
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
        {isLoading || !agent ? (
          <p className="text-sm text-muted-foreground">
            {t("agents.errors.loadAgent")}
          </p>
        ) : (
          <AgentForm
            schema={updateAgentSchema}
            providers={providers}
            skills={skills}
            pending={updateAgentMutation.isPending}
            initialValues={{
              name: agent.name,
              role: agent.role,
              kind: agent.kind,
              type: agent.type,
              status: agent.status as UpdateAgentModel["status"],
              providerIds: agent.providerIds,
              skillIds: agent.skillIds,
              modelOverride: agent.modelOverride ?? "",
              systemPrompt: agent.systemPrompt,
              temperature: agent.temperature ?? undefined,
              maxTokens: agent.maxTokens ?? undefined,
              metadataJson: agent.metadataJson ?? "",
            }}
            onSubmit={handleSubmit}
          />
        )}
      </SectionCard>
    </>
  )
}
