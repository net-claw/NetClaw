import { createFileRoute } from "@tanstack/react-router"

import AgentTeamsCreatePage from "@/pages/auth/agent-teams/agent-teams-create-page"

export const Route = createFileRoute("/_auth/agent-teams/create")({
  component: AgentTeamsCreatePage,
})
