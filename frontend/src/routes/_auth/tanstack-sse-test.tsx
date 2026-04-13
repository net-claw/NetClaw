import { createFileRoute } from "@tanstack/react-router"

import TanstackSseTestPage from "@/pages/auth/tanstack-sse-test-page"

export const Route = createFileRoute("/_auth/tanstack-sse-test")({
  component: TanstackSseTestPage,
})
