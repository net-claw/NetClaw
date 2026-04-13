import { useNavigate } from "@tanstack/react-router"

import {
  updateAgentTeamSchema,
  type UpdateAgentTeamModel,
} from "@/@types/models"
import { PageHeaderCard } from "@/components/share/cards/page-header-card"
import { SectionCard } from "@/components/share/cards/section-card"
import { Button } from "@/components/ui/button"
import { useGetAgentList } from "@/hooks/api/use-agent"
import {
  useGetAgentTeamById,
  useUpdateAgentTeam,
} from "@/hooks/api/use-agent-team"
import { actionIcons, appIcons } from "@/lib/icons"
import { AgentTeamForm } from "@/pages/auth/agent-teams/agent-team-form"
import { useTranslation } from "react-i18next"

type AgentTeamsEditPageProps = {
  teamId: string
}

export default function AgentTeamsEditPage({
  teamId,
}: AgentTeamsEditPageProps) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const TeamsIcon = appIcons.agents
  const BackIcon = actionIcons.back
  const { data: team, isLoading } = useGetAgentTeamById(teamId)
  const { data: agentsData } = useGetAgentList({
    pageIndex: 0,
    pageSize: 100,
    ascending: true,
    orderBy: "name",
    status: "active",
  })
  const updateAgentTeamMutation = useUpdateAgentTeam()

  const agents = agentsData?.items ?? []

  const handleSubmit = async (values: UpdateAgentTeamModel) => {
    await updateAgentTeamMutation.mutateAsync({
      teamId,
      payload: values,
    })
  }

  return (
    <>
      <PageHeaderCard
        icon={<TeamsIcon />}
        title={t("agentTeams.sheet.editTitle")}
        description={t("agentTeams.sheet.editDescription")}
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
        {isLoading || !team ? (
          <p className="text-sm text-muted-foreground">
            {t("agentTeams.errors.loadAgentTeam")}
          </p>
        ) : (
          <AgentTeamForm
            schema={updateAgentTeamSchema}
            pending={updateAgentTeamMutation.isPending}
            agents={agents}
            initialValues={{
              name: team.name,
              description: team.description ?? "",
              status: team.status,
              metadataJson: team.metadataJson ?? "",
              members: team.members.map((member) => ({
                id: member.id,
                agentId: member.agentId,
                role: member.role ?? "",
                order: member.order,
                status: member.status,
                reportsToMemberId: member.reportsToMemberId ?? "",
                metadataJson: member.metadataJson ?? "",
              })),
            }}
            onSubmit={handleSubmit}
          />
        )}
      </SectionCard>
    </>
  )
}
