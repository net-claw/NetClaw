import { createFileRoute } from "@tanstack/react-router"

import AgentsCreatePage from "@/pages/auth/agents/agents-create-page"

export const Route = createFileRoute("/_auth/agents/create")({
  component: AgentsCreatePage,
})
