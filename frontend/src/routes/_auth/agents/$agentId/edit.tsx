import { createFileRoute } from "@tanstack/react-router"

import AgentsEditPage from "@/pages/auth/agents/agents-edit-page"

function AgentsEditRoute() {
  const { agentId } = Route.useParams()

  return <AgentsEditPage agentId={agentId} />
}

export const Route = createFileRoute("/_auth/agents/$agentId/edit")({
  component: AgentsEditRoute,
})
