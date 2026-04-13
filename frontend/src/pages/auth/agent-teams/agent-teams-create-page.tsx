import { useNavigate } from "@tanstack/react-router"

import {
  createAgentTeamSchema,
  type CreateAgentTeamModel,
} from "@/@types/models"
import { PageHeaderCard } from "@/components/share/cards/page-header-card"
import { SectionCard } from "@/components/share/cards/section-card"
import { Button } from "@/components/ui/button"
import { useCreateAgentTeam } from "@/hooks/api/use-agent-team"
import { useGetAgentList } from "@/hooks/api/use-agent"
import { actionIcons, appIcons } from "@/lib/icons"
import { AgentTeamForm } from "@/pages/auth/agent-teams/agent-team-form"
import { useTranslation } from "react-i18next"

export default function AgentTeamsCreatePage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const createAgentTeamMutation = useCreateAgentTeam()
  const TeamsIcon = appIcons.agents
  const BackIcon = actionIcons.back
  const { data: agentsData } = useGetAgentList({
    pageIndex: 0,
    pageSize: 100,
    ascending: true,
    orderBy: "name",
    status: "active",
  })

  const agents = agentsData?.items ?? []

  const handleSubmit = async (values: CreateAgentTeamModel) => {
    const team = await createAgentTeamMutation.mutateAsync(values)
    navigate({
      to: "/agent-teams/$teamId/edit",
      params: { teamId: team.id },
    })
  }

  return (
    <>
      <PageHeaderCard
        icon={<TeamsIcon />}
        title={t("agentTeams.sheet.createTitle")}
        description={t("agentTeams.sheet.createDescription")}
        headerRight={
          <Button
            type="button"
            variant="outline"
            onClick={() => navigate({ to: "/agent-teams" })}
          >
            <BackIcon data-icon="inline-start" />
            {t("agentTeams.sheet.cancel")}
          </Button>
        }
      />

      <SectionCard contentClassName="flex flex-col gap-6">
        <AgentTeamForm
          schema={createAgentTeamSchema}
          pending={createAgentTeamMutation.isPending}
          agents={agents}
          onSubmit={handleSubmit}
        />
      </SectionCard>
    </>
  )
}
