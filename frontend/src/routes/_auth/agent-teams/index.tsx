import { createFileRoute } from "@tanstack/react-router"

import AgentTeamsListPage from "@/pages/auth/agent-teams/agent-teams-list-page"

export const Route = createFileRoute("/_auth/agent-teams/")({
  component: AgentTeamsListPage,
})
