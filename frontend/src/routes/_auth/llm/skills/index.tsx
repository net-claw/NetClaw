import { createFileRoute } from "@tanstack/react-router"

import SkillsListPage from "@/pages/auth/skills/skills-list-page"

export const Route = createFileRoute("/_auth/llm/skills/")({
  component: SkillsListPage,
})
