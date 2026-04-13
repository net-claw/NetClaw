import type { ReactNode } from "react"
import { useTranslation } from "react-i18next"

import { AppSidebar } from "@/components/app-sidebar"
import { LanguageSwitcher } from "@/components/language-switcher"
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb"
import { Separator } from "@/components/ui/separator"
import {
  SidebarInset,
  SidebarProvider,
  SidebarTrigger,
} from "@/components/ui/sidebar"

type AppShellProps = {
  section?: string
  title: string
  actions?: ReactNode
  children: ReactNode
}

export function AppShell({
  section = "NetClaw",
  title,
  actions,
  children,
}: AppShellProps) {
  const { t } = useTranslation()

  return (
    <SidebarProvider>
      <AppSidebar />
      <SidebarInset className="bg-sidebar">
        <header className="sticky top-0 z-20 flex h-18 shrink-0 items-center justify-between gap-2 bg-sidebar">
          <div className="flex w-full items-center justify-between gap-3 px-5 py-4">
            <div className="flex items-center gap-2">
              <SidebarTrigger className="-ml-1 rounded-full border text-foreground" />
              <Separator
                orientation="vertical"
                className="mr-2 hidden data-[orientation=vertical]:h-8 md:block"
              />
              <Breadcrumb className="hidden md:flex">
                <BreadcrumbList>
                  <BreadcrumbItem className="hidden md:block">
                    <BreadcrumbLink href="#">
                      {section === "NetClaw" ? t("shell.appName") : section}
                    </BreadcrumbLink>
                  </BreadcrumbItem>
                  <BreadcrumbSeparator className="hidden md:block" />
                  <BreadcrumbItem>
                    <BreadcrumbPage className="font-semibold text-foreground">
                      {title}
                    </BreadcrumbPage>
                  </BreadcrumbItem>
                </BreadcrumbList>
              </Breadcrumb>
            </div>
            <div className="flex items-center gap-2 rounded-full bg-card px-2 py-1 shadow-[0_18px_44px_-28px_rgba(80,87,120,0.38)] backdrop-blur">
              <LanguageSwitcher />
              {actions}
            </div>
          </div>
        </header>
        <div className="flex flex-1 flex-col overflow-hidden px-0">
          <div className="flex flex-1 flex-col overflow-auto rounded-none bg-app-main bg-cover bg-fixed bg-no-repeat lg:rounded-tl-2xl">
            <div className="mx-auto flex w-full max-w-360 flex-1 flex-col gap-8 px-5 py-6 md:px-7 md:py-7">
              {children}
            </div>
          </div>
        </div>
      </SidebarInset>
    </SidebarProvider>
  )
}
