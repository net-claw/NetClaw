"use client"

import { useLocation } from "@tanstack/react-router"
import * as React from "react"

import { NavMain } from "@/components/nav-main"
import { NavUser } from "@/components/nav-user"
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarHeader,
  SidebarMenuButton,
  SidebarRail,
} from "@/components/ui/sidebar"
import { useSidebar } from "@/components/ui/sidebar-context"
import { appSidebarSections } from "@/constants/sidebar"
import LogoIcon from "@/icons/logo-icon"

// This is sample data.
const data = {
  user: {
    name: "shadcn",
    email: "m@example.com",
    avatar: "/avatars/shadcn.jpg",
  },
  team: {
    name: "NetClaw",
    logo: <LogoIcon className="size-8 rounded-lg shadow-sm" />,
    plan: "Enterprise Agent",
  },
}

function SidebarNavigationSync() {
  const location = useLocation()
  const { isMobile, openMobile, setOpenMobile } = useSidebar()
  const previousPathnameRef = React.useRef(location.pathname)
  React.useEffect(() => {
    if (
      isMobile &&
      openMobile &&
      previousPathnameRef.current !== location.pathname
    ) {
      setOpenMobile(false)
    }

    previousPathnameRef.current = location.pathname
  }, [isMobile, location.pathname, openMobile, setOpenMobile])

  return null
}

export function AppSidebar({ ...props }: React.ComponentProps<typeof Sidebar>) {
  const { team } = data

  return (
    <Sidebar collapsible="icon" {...props}>
      <SidebarNavigationSync />
      <SidebarHeader>
        <SidebarMenuButton
          size="lg"
          className="data-[state=open]:bg-sidebar-accent data-[state=open]:text-sidebar-accent-foreground"
        >
          {team.logo}
          <div className="grid flex-1 text-left text-sm leading-tight">
            <span className="truncate font-medium">{team.name}</span>
            <span className="truncate text-xs">{team.plan}</span>
          </div>
        </SidebarMenuButton>
      </SidebarHeader>
      <SidebarContent>
        <NavMain sections={appSidebarSections} />
      </SidebarContent>
      <SidebarFooter>
        <NavUser />
      </SidebarFooter>
      <SidebarRail />
    </Sidebar>
  )
}
