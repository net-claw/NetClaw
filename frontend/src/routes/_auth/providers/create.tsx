import { createFileRoute } from "@tanstack/react-router"

import ProvidersCreatePage from "@/pages/auth/providers/providers-create-page"

export const Route = createFileRoute("/_auth/providers/create")({
  component: ProvidersCreatePage,
})
