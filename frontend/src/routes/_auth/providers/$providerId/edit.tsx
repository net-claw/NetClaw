import { createFileRoute } from "@tanstack/react-router"

import ProvidersEditPage from "@/pages/auth/providers/providers-edit-page"

function ProvidersEditRoute() {
  const { providerId } = Route.useParams()

  return <ProvidersEditPage providerId={providerId} />
}

export const Route = createFileRoute("/_auth/providers/$providerId/edit")({
  component: ProvidersEditRoute,
})
