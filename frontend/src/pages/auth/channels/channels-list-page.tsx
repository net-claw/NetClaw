import { Link } from "@tanstack/react-router"
import { useMemo, useState } from "react"
import { useTranslation } from "react-i18next"

import { DeleteConfirmDialog } from "@/components/delete-confirm-dialog"
import { PageHeaderCard } from "@/components/share/cards/page-header-card"
import { SectionCard } from "@/components/share/cards/section-card"
import { Button } from "@/components/ui/button"
import {
  InputGroup,
  InputGroupAddon,
  InputGroupInput,
  InputGroupText,
} from "@/components/ui/input-group"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import {
  useDeleteChannels,
  useGetChannelList,
  useRestartChannel,
  useStartChannel,
  useStopChannel,
} from "@/hooks/api/use-channel"
import { actionIcons, appIcons } from "@/lib/icons"

export default function ChannelsListPage() {
  const { t } = useTranslation()
  const [query, setQuery] = useState("")
  const [selectedIds, setSelectedIds] = useState<string[]>([])
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false)
  const ChannelsIcon = appIcons.channels
  const CreateIcon = actionIcons.create
  const DeleteIcon = actionIcons.delete
  const SearchIcon = actionIcons.search
  const RefreshIcon = actionIcons.refresh
  const StartIcon = actionIcons.start
  const StopIcon = actionIcons.stop
  const RestartIcon = actionIcons.restart
  const deleteChannelsMutation = useDeleteChannels()
  const startChannelMutation = useStartChannel()
  const stopChannelMutation = useStopChannel()
  const restartChannelMutation = useRestartChannel()

  const { data, isLoading, isFetching, refetch } = useGetChannelList({
    pageIndex: 0,
    pageSize: 50,
    searchText: query.trim() || undefined,
    orderBy: "updatedAt",
    ascending: false,
  })

  const channels = data?.items ?? []
  const totalItems = data?.totalItems ?? 0
  const selectedIdSet = useMemo(() => new Set(selectedIds), [selectedIds])

  return (
    <>
      <PageHeaderCard
        icon={<ChannelsIcon />}
        title={t("channels.page.title")}
        description={t("channels.page.description")}
        titleMeta={totalItems}
        headerRight={
          <Button asChild size="lg">
            <Link to="/channels/create">
              <CreateIcon data-icon="inline-start" />
              {t("channels.actions.newChannel")}
            </Link>
          </Button>
        }
      />

      <SectionCard
        headerRight={
          selectedIds.length > 0 ? (
            <DeleteConfirmDialog
              open={isDeleteDialogOpen}
              onOpenChange={setIsDeleteDialogOpen}
              title={t("channels.deleteDialog.title")}
              description={t("channels.deleteDialog.description_other", {
                count: selectedIds.length,
              })}
              confirmLabel={t("channels.deleteDialog.confirm")}
              cancelLabel={t("channels.deleteDialog.cancel")}
              onConfirm={async () => {
                await deleteChannelsMutation.mutateAsync(selectedIds)
                setSelectedIds([])
                setIsDeleteDialogOpen(false)
              }}
              trigger={
                <Button type="button" size="sm" variant="destructive">
                  <DeleteIcon data-icon="inline-start" />
                  {t("channels.actions.deleteSelected_other", {
                    count: selectedIds.length,
                  })}
                </Button>
              }
            />
          ) : null
        }
      >
        <div className="flex flex-col gap-4">
          <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
            <div className="w-full max-w-xl">
              <InputGroup>
                <InputGroupAddon>
                  <InputGroupText>
                    <SearchIcon />
                  </InputGroupText>
                </InputGroupAddon>
                <InputGroupInput
                  value={query}
                  placeholder={t("channels.searchPlaceholder")}
                  onChange={(event) => setQuery(event.target.value)}
                />
              </InputGroup>
            </div>

            <Button
              type="button"
              variant="outline"
              size="sm"
              disabled={isFetching}
              onClick={() => {
                void refetch()
              }}
            >
              <RefreshIcon data-icon="inline-start" />
              {t("identity.toolbar.refresh")}
            </Button>
          </div>

          <Table>
            <TableHeader>
              <TableRow>
                <TableHead />
                <TableHead>{t("channels.table.name")}</TableHead>
                <TableHead>{t("channels.table.kind")}</TableHead>
                <TableHead>{t("channels.table.status")}</TableHead>
                <TableHead>{t("channels.table.updatedAt")}</TableHead>
                <TableHead>{t("channels.table.actions")}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {channels.map((channel) => (
                <TableRow key={channel.id}>
                  <TableCell>
                    <input
                      type="checkbox"
                      checked={selectedIdSet.has(channel.id)}
                      onChange={(event) => {
                        setSelectedIds((current) =>
                          event.target.checked
                            ? [...current, channel.id]
                            : current.filter((id) => id !== channel.id)
                        )
                      }}
                    />
                  </TableCell>
                  <TableCell className="font-medium">{channel.name}</TableCell>
                  <TableCell>{channel.kind}</TableCell>
                  <TableCell>{channel.status}</TableCell>
                  <TableCell>{channel.updatedOn ?? channel.createdOn}</TableCell>
                  <TableCell>
                    <div className="flex flex-wrap gap-2">
                      <Button asChild size="sm" variant="outline">
                        <Link
                          to="/channels/$channelId/edit"
                          params={{ channelId: channel.id }}
                        >
                          {t("identity.actions.edit")}
                        </Link>
                      </Button>
                      <Button
                        type="button"
                        size="sm"
                        variant="outline"
                        disabled={startChannelMutation.isPending}
                        onClick={() => {
                          void startChannelMutation.mutateAsync(channel.id)
                        }}
                      >
                        <StartIcon data-icon="inline-start" />
                        {t("channels.actions.start")}
                      </Button>
                      <Button
                        type="button"
                        size="sm"
                        variant="outline"
                        disabled={stopChannelMutation.isPending}
                        onClick={() => {
                          void stopChannelMutation.mutateAsync(channel.id)
                        }}
                      >
                        <StopIcon data-icon="inline-start" />
                        {t("channels.actions.stop")}
                      </Button>
                      <Button
                        type="button"
                        size="sm"
                        variant="outline"
                        disabled={restartChannelMutation.isPending}
                        onClick={() => {
                          void restartChannelMutation.mutateAsync(channel.id)
                        }}
                      >
                        <RestartIcon data-icon="inline-start" />
                        {t("channels.actions.restart")}
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))}

              {!isLoading && channels.length === 0 ? (
                <TableRow>
                  <TableCell
                    colSpan={6}
                    className="text-center text-sm text-muted-foreground"
                  >
                    {t("channels.emptyChannels")}
                  </TableCell>
                </TableRow>
              ) : null}
            </TableBody>
          </Table>
        </div>
      </SectionCard>
    </>
  )
}
