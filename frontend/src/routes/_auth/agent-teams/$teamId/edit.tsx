import { createFileRoute } from "@tanstack/react-router"

import AgentTeamsEditPage from "@/pages/auth/agent-teams/agent-teams-edit-page"

function AgentTeamEditRoute() {
  const { teamId } = Route.useParams()

  return <AgentTeamsEditPage teamId={teamId} />
}

export const Route = createFileRoute("/_auth/agent-teams/$teamId/edit")({
  component: AgentTeamEditRoute,
})
