import {
  ArrowDownToLineIcon,
  ArrowUpRightIcon,
  BellIcon,
  ChevronDownIcon,
  CircleIcon,
  DropletsIcon,
  GaugeIcon,
  MoreHorizontalIcon,
} from "lucide-react"
import { useTranslation } from "react-i18next"

import { Button } from "@/components/ui/button"
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { cn } from "@/lib/utils"

const chartPrimaryPoints = [66, 78, 72, 84, 76, 74, 79, 81, 75, 83, 77, 86]
const chartSecondaryPoints = [34, 39, 32, 44, 31, 29, 35, 33, 41, 36, 47, 40]

const familyMembers = [
  {
    name: "Lance Douglas",
    initials: "LD",
    usage: "1160 / 2000 L",
    state: "normal temp",
    progress: 58,
    tone:
      "bg-[#f4d2cc] text-[#6f4038] ring-1 ring-white/70 shadow-[inset_0_1px_0_rgba(255,255,255,0.55)]",
  },
  {
    name: "Cora Douglas",
    initials: "CD",
    usage: "2330 / 3000 L",
    state: "too fast",
    progress: 78,
    tone:
      "bg-[#ddd8ff] text-[#5950aa] ring-1 ring-white/70 shadow-[inset_0_1px_0_rgba(255,255,255,0.55)]",
  },
  {
    name: "Shelia Douglas",
    initials: "SD",
    usage: "1960 / 3000 L",
    state: "above normal",
    progress: 65,
    tone:
      "bg-[#dff0ff] text-[#45638f] ring-1 ring-white/70 shadow-[inset_0_1px_0_rgba(255,255,255,0.55)]",
  },
  {
    name: "Joseph Douglas",
    initials: "JD",
    usage: "910 / 2000 L",
    state: "normal temp",
    progress: 46,
    tone:
      "bg-[#d8eef3] text-[#356372] ring-1 ring-white/70 shadow-[inset_0_1px_0_rgba(255,255,255,0.55)]",
  },
]

function buildChartPath(points: number[]) {
  return points
    .map((point, index) => {
      const x = (index / (points.length - 1)) * 100
      const y = 100 - point
      return `${index === 0 ? "M" : "L"} ${x} ${y}`
    })
    .join(" ")
}

function TinyTrend({
  value,
  label,
  accent,
}: {
  value: string
  label: string
  accent: string
}) {
  return (
    <div className="flex min-w-0 flex-1 flex-col gap-2 rounded-[1.35rem] border border-border/70 bg-muted/65 p-4 dark:border-white/6 dark:bg-white/[0.03]">
      <div
        className="h-8 w-0.5 rounded-full"
        style={{ backgroundColor: accent }}
      />
      <div className="space-y-1">
        <p className="text-xs font-medium tracking-[0.18em] text-muted-foreground uppercase">
          {label}
        </p>
        <p className="text-xl font-semibold tracking-[-0.04em]">{value}</p>
      </div>
    </div>
  )
}

function DashboardChart() {
  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between gap-4">
        <div>
          <p className="text-xs font-medium tracking-[0.18em] text-muted-foreground uppercase">
            Water consumption
          </p>
          <h2 className="mt-3 text-[clamp(2rem,4vw,3.15rem)] font-semibold tracking-[-0.06em] text-foreground">
            125.799,54 L
          </h2>
        </div>

        <button className="inline-flex items-center gap-3 rounded-xl border border-border bg-white px-4 py-2 text-sm font-medium text-foreground shadow-[0_14px_34px_-28px_rgba(70,76,104,0.45)] dark:border-white/10 dark:bg-white/[0.06] dark:text-white dark:shadow-none">
          All zones
          <ChevronDownIcon className="size-4 text-muted-foreground" />
        </button>
      </div>

      <div className="flex justify-end gap-4 text-[11px] font-semibold tracking-[0.18em] text-muted-foreground uppercase">
        {["1H", "24H", "1W", "1M", "1Y", "ALL"].map((range) => (
          <span
            key={range}
            className={cn(
              "transition-colors",
              range === "1Y" ? "text-foreground" : "text-muted-foreground"
            )}
          >
            {range}
          </span>
        ))}
      </div>

      <div className="grid grid-cols-[auto_1fr] gap-x-4 gap-y-2">
        <div className="flex flex-col justify-between py-2 text-[11px] font-medium text-muted-foreground">
          <span>10L</span>
          <span>8L</span>
          <span>6L</span>
          <span>4L</span>
          <span>2L</span>
          <span>0L</span>
        </div>

        <div className="relative h-72 overflow-hidden rounded-[1.75rem] border border-border/70 bg-[linear-gradient(180deg,rgba(255,255,255,0.8)_0%,rgba(245,247,251,0.95)_100%)] px-2 py-3 dark:border-white/8 dark:bg-[linear-gradient(180deg,rgba(255,255,255,0.05)_0%,rgba(255,255,255,0.02)_100%)]">
          <div className="absolute inset-x-0 top-0 h-full">
            {[0, 20, 40, 60, 80, 100].map((line) => (
              <div
                key={line}
                className="absolute right-0 left-0 border-t border-border/70 dark:border-white/8"
                style={{ top: `${line}%` }}
              />
            ))}
          </div>

          <div className="absolute top-3 bottom-11 left-[21.6%] border-l border-dashed border-primary/30 dark:border-[color:var(--color-chart-2)]/60" />

          <svg
            viewBox="0 0 100 100"
            preserveAspectRatio="none"
            className="absolute inset-x-2 top-3 h-[calc(100%-3.5rem)] w-[calc(100%-1rem)]"
          >
            <path
              d={buildChartPath(chartPrimaryPoints)}
              fill="none"
              stroke="var(--color-chart-1)"
              strokeWidth="1.1"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
            <path
              d={buildChartPath(chartSecondaryPoints)}
              fill="none"
              stroke="var(--color-chart-2)"
              strokeWidth="1.1"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
            <circle cx="18.18" cy="28" r="1.6" fill="var(--color-chart-1)" />
          </svg>

          <div className="absolute top-[5.5rem] left-[19%] rounded-full bg-white px-2.5 py-1 text-xs font-semibold shadow-[0_12px_30px_-22px_rgba(70,76,104,0.45)] dark:bg-[#27293c] dark:text-white dark:shadow-none">
            8.930L
          </div>

          <div className="absolute right-4 bottom-2 left-4 grid grid-cols-12 text-[11px] font-medium text-muted-foreground">
            {["Aug", "Sep", "Oct", "Nov", "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", ""].map(
              (month, index) => (
                <span key={`${month}-${index}`}>{month}</span>
              )
            )}
          </div>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <TinyTrend value="36.501,12 L" label="Drinking" accent="#8f96ff" />
        <TinyTrend
          value="89.298,42 L"
          label="Technical water"
          accent="#b9bdfd"
        />
      </div>
    </div>
  )
}

function BalanceGauge() {
  return (
    <div className="space-y-5">
      <div className="relative mx-auto flex h-44 w-44 items-center justify-center overflow-hidden rounded-full">
        <div className="absolute inset-0 rounded-full bg-[conic-gradient(from_220deg,#eceffd_0deg,#d7dbff_110deg,#a1a8ff_210deg,#8f96ff_285deg,#eef1fb_320deg,#eef1fb_360deg)] dark:bg-[conic-gradient(from_220deg,#d8dbfb_0deg,#c7c3ff_110deg,#9da4ff_210deg,#8e96ff_285deg,#444763_320deg,#444763_360deg)]" />
        <div className="absolute inset-5 rounded-full bg-card" />
        <div className="absolute top-1/2 left-1/2 h-1.5 w-14 -translate-x-1/2 -translate-y-1/2 origin-left rotate-[24deg] rounded-full bg-[#8f96ff]" />
        <div className="absolute top-1/2 left-1/2 size-4 -translate-x-1/2 -translate-y-1/2 rounded-full bg-muted shadow-[0_0_0_6px_rgba(245,247,251,1)] dark:shadow-[0_0_0_6px_rgba(26,28,45,1)]" />
      </div>

      <div className="flex items-end justify-between gap-3 border-t border-border/80 pt-4">
        <div>
          <p className="text-sm text-muted-foreground">Invoice</p>
          <p className="mt-1 text-3xl font-semibold tracking-[-0.05em]">
            $678.67
          </p>
        </div>

        <div className="flex items-center gap-2">
          <Button variant="ghost" size="icon-sm" className="rounded-xl">
            <ArrowDownToLineIcon className="size-4" />
          </Button>
          <Button className="rounded-xl bg-[#17181c] px-5 text-white hover:bg-[#25272f] dark:bg-primary dark:text-primary-foreground dark:hover:bg-primary/90">
            Pay
          </Button>
        </div>
      </div>
    </div>
  )
}

export default function DashboardPage() {
  const { t } = useTranslation()

  return (
    <div className="grid gap-6 xl:grid-cols-[minmax(0,1.65fr)_340px]">
      <Card className="overflow-hidden rounded-[2rem] border border-white/70 bg-card/96 shadow-panel dark:border-white/6 dark:bg-card">
        <CardHeader className="gap-5 border-b border-border/70 pb-6 dark:border-white/6">
          <div className="flex flex-col gap-5 lg:flex-row lg:items-center lg:justify-between">
            <div>
              <p className="text-xs font-medium tracking-[0.22em] text-muted-foreground uppercase">
                {t("common.dashboard")}
              </p>
              <CardTitle className="mt-3 text-[2rem] tracking-[-0.06em]">
                Water control overview
              </CardTitle>
              <CardDescription className="mt-2 max-w-2xl text-sm leading-6">
                A quieter dark dashboard with soft off-white actions and cool
                chart accents for the main data moments.
              </CardDescription>
            </div>

            <div className="flex flex-wrap items-center gap-3">
              <div className="flex items-center gap-3 rounded-[1.35rem] border border-border/70 bg-muted/75 px-4 py-3 shadow-[0_16px_34px_-28px_rgba(70,76,104,0.35)] dark:border-white/8 dark:bg-[#0f1120] dark:shadow-none">
                <div className="h-11 w-11 rounded-full bg-[linear-gradient(135deg,#dbe4ff,#f2f4ff)] dark:bg-[linear-gradient(135deg,#bcc2ff,#eceeff)]" />
                <div>
                  <p className="text-sm font-semibold">Family Villa</p>
                  <p className="text-xs text-muted-foreground">
                    620 Charles St. South Plainfield
                  </p>
                </div>
              </div>

              <Button
                variant="outline"
                size="icon-sm"
                className="rounded-full border-border bg-white shadow-[0_14px_34px_-28px_rgba(70,76,104,0.42)] dark:border-white/10 dark:bg-[#0f1120] dark:text-white dark:shadow-none"
              >
                <BellIcon className="size-4" />
              </Button>
            </div>
          </div>
        </CardHeader>

        <CardContent className="pt-6">
          <DashboardChart />

          <div className="mt-8 border-t border-border/70 pt-7 dark:border-white/6">
            <div className="mb-6 flex items-center justify-between gap-4">
              <div>
                <h3 className="text-xl font-semibold tracking-[-0.04em]">
                  Family members
                </h3>
                <p className="mt-1 text-sm text-muted-foreground">
                  Consumption pace and current flow behaviour.
                </p>
              </div>

              <div className="flex items-center gap-3 text-[11px] font-semibold tracking-[0.18em] text-muted-foreground uppercase">
                <span>1W</span>
                <span className="text-foreground">1M</span>
                <span>1Y</span>
              </div>
            </div>

            <div className="space-y-4">
              {familyMembers.map((member) => (
                <div
                  key={member.name}
                  className="grid items-center gap-4 rounded-[1.35rem] border border-border/70 bg-muted/55 px-4 py-3 md:grid-cols-[minmax(0,1.2fr)_150px_minmax(0,1fr)_140px] dark:border-white/6 dark:bg-white/[0.03]"
                >
                  <div className="flex items-center gap-3">
                    <div
                      className={cn(
                        "flex size-10 items-center justify-center rounded-full text-sm font-semibold",
                        member.tone
                      )}
                    >
                      {member.initials}
                    </div>
                    <div>
                      <p className="font-medium">{member.name}</p>
                      <p className="text-xs text-muted-foreground">
                        Daily allocation tracking
                      </p>
                    </div>
                  </div>

                  <p className="text-sm font-semibold">{member.usage}</p>

                  <div className="h-2.5 rounded-full bg-white dark:bg-white/8">
                    <div
                      className="h-full rounded-full bg-[linear-gradient(90deg,#dfe2ff,#959cff)] dark:bg-[linear-gradient(90deg,#d6d9ff,#9ca4ff)]"
                      style={{ width: `${member.progress}%` }}
                    />
                  </div>

                  <div className="flex items-center justify-between gap-2 text-sm text-foreground">
                    <span className="inline-flex items-center gap-2">
                      <CircleIcon className="size-3 fill-current text-primary" />
                      {member.state}
                    </span>
                    <MoreHorizontalIcon className="size-4 text-muted-foreground" />
                  </div>
                </div>
              ))}
            </div>
          </div>
        </CardContent>
      </Card>

      <div className="flex flex-col gap-6">
        <Card className="rounded-[1.8rem] border border-white/70 bg-card/96 shadow-panel dark:border-white/6 dark:bg-card">
          <CardHeader className="pb-4">
            <div className="flex items-center justify-between gap-3">
              <div>
                <p className="text-xs font-medium tracking-[0.18em] text-muted-foreground uppercase">
                  Basic plan
                </p>
                <CardTitle className="mt-3 text-[2rem] tracking-[-0.06em]">
                  10.000L
                </CardTitle>
              </div>

              <div className="flex h-10 w-16 items-center rounded-full bg-muted p-1">
                <div className="ml-auto size-8 rounded-full bg-white shadow-[0_10px_24px_-18px_rgba(70,76,104,0.55)]" />
              </div>
            </div>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex items-end justify-between gap-3">
              <div>
                <p className="text-2xl font-semibold tracking-[-0.05em]">
                  $1350
                </p>
                <p className="text-sm text-muted-foreground">monthly</p>
              </div>
              <Button variant="ghost" className="rounded-xl px-0 text-xs font-semibold tracking-[0.18em] uppercase">
                Change plan
                <ArrowUpRightIcon className="size-3.5" />
              </Button>
            </div>
          </CardContent>
        </Card>

        <Card className="rounded-[1.8rem] border border-white/70 bg-card/96 shadow-panel dark:border-white/6 dark:bg-card">
          <CardHeader className="pb-4">
            <p className="text-xs font-medium tracking-[0.18em] text-muted-foreground uppercase">
              Water balance
            </p>
            <CardTitle className="mt-3 text-[2.4rem] tracking-[-0.06em]">
              3.640,55 L
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-5">
            <div className="rounded-[1.35rem] border border-border bg-muted/55 p-4 dark:border-white/8 dark:bg-white/[0.03]">
              <p className="text-sm text-muted-foreground">Credit limit</p>
              <div className="mt-3 flex items-center justify-between gap-3">
                <div>
                  <p className="text-2xl font-semibold tracking-[-0.04em]">
                    5.000L
                  </p>
                  <p className="text-xs text-muted-foreground">$78 / 1L</p>
                </div>
                <Button variant="outline" className="rounded-xl bg-white dark:border-white/10 dark:bg-white/[0.04] dark:text-white dark:hover:bg-white/[0.08]">
                  Activate
                </Button>
              </div>
            </div>

            <BalanceGauge />
          </CardContent>
        </Card>

        <Card className="rounded-[1.8rem] border border-white/70 bg-[linear-gradient(145deg,rgba(241,243,255,0.96),rgba(232,238,255,0.98))] shadow-panel dark:border-white/6 dark:bg-[linear-gradient(145deg,rgba(45,47,69,0.96),rgba(25,27,42,0.98))]">
          <CardContent className="flex items-end justify-between gap-4 py-6">
            <div className="space-y-3">
              <div className="inline-flex size-12 items-center justify-center rounded-2xl bg-white/80 text-primary shadow-[0_16px_34px_-26px_rgba(70,76,104,0.4)] dark:bg-white/10 dark:text-white dark:shadow-none">
                <DropletsIcon className="size-5" />
              </div>
              <div>
                <p className="max-w-[12rem] text-3xl font-semibold tracking-[-0.06em]">
                  Let&apos;s get some help
                </p>
                <p className="mt-2 text-sm text-muted-foreground">
                  Donate water to emergency zones.
                </p>
              </div>
            </div>

            <Button size="icon" className="rounded-2xl bg-white text-foreground shadow-[0_16px_34px_-26px_rgba(70,76,104,0.35)] hover:bg-white/95 dark:bg-primary dark:text-primary-foreground dark:shadow-none dark:hover:bg-primary/90">
              <GaugeIcon className="size-4" />
            </Button>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
