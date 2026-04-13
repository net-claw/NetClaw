import { createFileRoute } from "@tanstack/react-router"

import ProvidersListPage from "@/pages/auth/providers/providers-list-page"

export const Route = createFileRoute("/_auth/providers/")({
  component: ProvidersListPage,
})
