import DockerIcon from "@/icons/docker-icon"
import GitIcon from "@/icons/git-icon"
import {
  Box,
  BoxMinimalistic,
  ChatRoundDots,
  Cpu,
  Database,
  Global,
  HomeSmile,
  Lightning,
  PlugCircle,
  Settings,
  ShieldCheck,
  UsersGroupRounded,
  UsersGroupTwoRounded,
} from "@solar-icons/react"
import {
  ArrowLeftIcon,
  CopyIcon,
  FilterIcon,
  PlayIcon,
  PlusIcon,
  RefreshCwIcon,
  SearchIcon,
  Settings2Icon,
  SquareIcon,
  SquarePenIcon,
  Trash2Icon,
  UploadIcon,
} from "lucide-react"
import type { ComponentProps, ComponentType } from "react"

type SolarIconProps = ComponentProps<typeof HomeSmile>

function createSolarIcon(Icon: ComponentType<SolarIconProps>) {
  return function AppSolarIcon({
    className = "size-6",
    weight = "Outline",
    ...props
  }: SolarIconProps) {
    return <Icon className={className} weight={weight} {...props} />
  }
}

export const appIcons = {
  builds: createSolarIcon(Lightning),
  docker: DockerIcon,
  databases: createSolarIcon(Database),
  domains: createSolarIcon(Global),
  projects: createSolarIcon(Box),
  sources: GitIcon,
  settings: createSolarIcon(Settings),
  dashboard: createSolarIcon(HomeSmile),
  channels: createSolarIcon(ChatRoundDots),
  users: createSolarIcon(UsersGroupRounded),
  roles: createSolarIcon(ShieldCheck),
  providers: createSolarIcon(PlugCircle),
  agents: createSolarIcon(BoxMinimalistic),
  agentTeams: createSolarIcon(UsersGroupTwoRounded),

  skills: createSolarIcon(Cpu),
} as const

export const actionIcons = {
  back: ArrowLeftIcon,
  copy: CopyIcon,
  create: PlusIcon,
  delete: Trash2Icon,
  edit: SquarePenIcon,
  filter: FilterIcon,
  refresh: RefreshCwIcon,
  restart: RefreshCwIcon,
  search: SearchIcon,
  settings: Settings2Icon,
  start: PlayIcon,
  stop: SquareIcon,
  upload: UploadIcon,
} as const
