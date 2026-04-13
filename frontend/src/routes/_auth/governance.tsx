import GovernancePage from "@/pages/auth/governance-page"
import { createFileRoute } from "@tanstack/react-router"

export const Route = createFileRoute("/_auth/governance")({
  component: GovernancePage,
})
