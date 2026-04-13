import {
  Outlet,
  createFileRoute,
  redirect,
  useRouterState,
} from "@tanstack/react-router"
import { useEffect } from "react"

import { AppShell } from "@/components/app-shell"
import { appSidebarSections } from "@/constants/sidebar"
import { IdentityShellActions } from "@/routes/_auth/users/components/-identity-shell-actions"
import { useAuthStore } from "@/store/auth"
import { useTranslation } from "react-i18next"

function getSidebarMatch(pathname: string) {
  for (const section of appSidebarSections) {
    for (const item of section.items) {
      if (pathname === item.url || pathname.startsWith(`${item.url}/`)) {
        return {
          sectionKey: section.labelKey,
          titleKey: item.titleKey,
        }
      }

      const childMatch = item.children?.find(
        (child) =>
          pathname === child.url || pathname.startsWith(`${child.url}/`)
      )

      if (childMatch) {
        return {
          sectionKey: section.labelKey,
          titleKey: childMatch.titleKey,
        }
      }
    }
  }

  return null
}

function AuthLayout() {
  const { t } = useTranslation()
  const pathname = useRouterState({
    select: (state) => state.location.pathname,
  })
  const sidebarMatch = getSidebarMatch(pathname)

  const section = sidebarMatch
    ? t(sidebarMatch.sectionKey)
    : t("shell.workspace")
  const title = sidebarMatch ? t(sidebarMatch.titleKey) : t("common.dashboard")

  useEffect(() => {
    const appName = t("shell.appName")
    document.title = title === appName ? appName : `${title} | ${appName}`
  }, [t, title])

  return (
    <AppShell
      section={section}
      title={title}
      actions={<IdentityShellActions />}
    >
      <Outlet />
    </AppShell>
  )
}

export const Route = createFileRoute("/_auth")({
  beforeLoad: async () => {
    const { isAuthenticated, init } = useAuthStore.getState()
    if (!isAuthenticated) {
      const ok = await init()
      if (!ok) throw redirect({ to: "/login" })
    }
  },
  component: AuthLayout,
})
