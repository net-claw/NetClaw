import { createFileRoute } from "@tanstack/react-router"

import SkillsEditPage from "@/pages/auth/skills/skills-edit-page"

export const Route = createFileRoute("/_auth/llm/skills/$skillId/edit")({
  component: RouteComponent,
})

function RouteComponent() {
  const { skillId } = Route.useParams()
  return <SkillsEditPage skillId={skillId} />
}
