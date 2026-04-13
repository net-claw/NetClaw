import { createFileRoute } from "@tanstack/react-router"

import SkillsCreatePage from "@/pages/auth/skills/skills-create-page"

export const Route = createFileRoute("/_auth/llm/skills/create")({
  component: SkillsCreatePage,
})
