import type { ComponentType } from "react"

import { appIcons } from "@/lib/icons"
export type SidebarChildItem = {
  titleKey: string
  url: string
}

export type SidebarItem = {
  titleKey: string
  url: string
  icon?: ComponentType<{ className?: string }>
  isActive?: boolean
  children?: SidebarChildItem[]
}

export type SidebarSection = {
  labelKey: string
  items: SidebarItem[]
}

export const appSidebarSections: SidebarSection[] = [
  {
    labelKey: "sidebar.platform",
    items: [
      {
        titleKey: "common.dashboard",
        url: "/dashboard",
        icon: appIcons.dashboard,
      },
      {
        titleKey: "common.tanstackSseTest",
        url: "/tanstack-sse-test",
        icon: appIcons.channels,
      },
      {
        titleKey: "sidebar.skills",
        url: "/llm/skills",
        icon: appIcons.skills,
      },
      {
        titleKey: "sidebar.governance",
        url: "/governance",
        icon: appIcons.settings,
      },
      {
        titleKey: "sidebar.providers",
        url: "/providers",
        icon: appIcons.providers,
      },
      {
        titleKey: "sidebar.channels",
        url: "/channels",
        icon: appIcons.channels,
      },
      {
        titleKey: "sidebar.agents",
        url: "/agents",
        icon: appIcons.agents,
      },
      {
        titleKey: "sidebar.agentTeams",
        url: "/agent-teams",
        icon: appIcons.agentTeams,
      },
    ],
  },
  {
    labelKey: "sidebar.identity",
    items: [
      {
        titleKey: "sidebar.users",
        url: "/users",
        icon: appIcons.users,
      },
      {
        titleKey: "sidebar.roles",
        url: "/roles",
        icon: appIcons.roles,
      },
    ],
  },
]
