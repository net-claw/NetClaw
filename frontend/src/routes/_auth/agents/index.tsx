import { createFileRoute } from "@tanstack/react-router"

import AgentsListPage from "@/pages/auth/agents/agents-list-page"

export const Route = createFileRoute("/_auth/agents/")({
  component: AgentsListPage,
})
