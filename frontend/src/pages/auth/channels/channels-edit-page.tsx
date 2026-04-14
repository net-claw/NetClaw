import type {
  ColumnDef,
  PaginationState,
} from "@tanstack/react-table"
import { useNavigate } from "@tanstack/react-router"
import { useMemo, useState } from "react"
import { useTranslation } from "react-i18next"

import {
  type ConversationListItemModel,
  updateChannelSchema,
  type UpdateChannelModel,
} from "@/@types/models"
import { DataTable } from "@/components/data-table/data-table"
import { DataTablePagination } from "@/components/data-table/data-table-pagination"
import { PageHeaderCard } from "@/components/share/cards/page-header-card"
import { SectionCard } from "@/components/share/cards/section-card"
import { Button } from "@/components/ui/button"
import { useGetChannelById, useUpdateChannel } from "@/hooks/api/use-channel"
import {
  useGetConversationById,
  useGetConversationList,
} from "@/hooks/api/use-conversation"
import { actionIcons, appIcons } from "@/lib/icons"
import { useDataTable } from "@/components/data-table/use-data-table"
import { ChannelForm } from "@/pages/auth/channels/channel-form"

type ChannelsEditPageProps = {
  channelId: string
}

const pageSizeOptions = [5, 10, 20] as const

function formatDateTime(value?: string | null) {
  if (!value) return "n/a"
  const parsed = new Date(value)
  return Number.isNaN(parsed.getTime())
    ? value
    : parsed.toLocaleString(undefined, {
        dateStyle: "short",
        timeStyle: "short",
      })
}

export default function ChannelsEditPage({
  channelId,
}: ChannelsEditPageProps) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const ChannelsIcon = appIcons.channels
  const BackIcon = actionIcons.back
  const [pagination, setPagination] = useState<PaginationState>({
    pageIndex: 0,
    pageSize: 5,
  })
  const [selectedConversationId, setSelectedConversationId] = useState("")
  const { data: channel, isLoading } = useGetChannelById(channelId)
  const updateChannelMutation = useUpdateChannel()
  const {
    data: conversations = [],
    isLoading: isLoadingConversations,
    isError: isConversationsError,
    isFetching: isFetchingConversations,
    refetch: refetchConversations,
  } = useGetConversationList({
    targetType: "channel",
    targetId: channelId,
  })
  const { data: selectedConversation, isLoading: isLoadingSelectedConversation } =
    useGetConversationById(selectedConversationId, Boolean(selectedConversationId))

  const pagedConversations = useMemo(() => {
    const start = pagination.pageIndex * pagination.pageSize
    return conversations.slice(start, start + pagination.pageSize)
  }, [conversations, pagination.pageIndex, pagination.pageSize])

  const conversationColumns = useMemo<ColumnDef<ConversationListItemModel>[]>(
    () => [
      {
        accessorKey: "title",
        header: "Title",
        cell: ({ row }) => row.original.title || row.original.externalId,
      },
      {
        accessorKey: "status",
        header: "Status",
      },
      {
        accessorKey: "externalId",
        header: "Thread ID",
        cell: ({ row }) => (
          <span className="font-mono text-xs">{row.original.externalId}</span>
        ),
      },
      {
        accessorKey: "lastMessageAt",
        header: "Last Message",
        cell: ({ row }) => formatDateTime(row.original.lastMessageAt),
      },
      {
        accessorKey: "createdAt",
        header: "Created",
        cell: ({ row }) => formatDateTime(row.original.createdAt),
      },
      {
        id: "actions",
        header: () => <div className="text-right">Actions</div>,
        cell: ({ row }) => (
          <div className="flex justify-end">
            <Button
              type="button"
              size="sm"
              variant={selectedConversationId === row.original.id ? "default" : "outline"}
              onClick={() => setSelectedConversationId(row.original.id)}
            >
              View chat
            </Button>
          </div>
        ),
      },
    ],
    [selectedConversationId]
  )

  const conversationsTable = useDataTable({
    data: pagedConversations,
    columns: conversationColumns,
    rowCount: conversations.length,
    manualPagination: true,
    onPaginationChange: setPagination,
    state: {
      pagination,
    },
  })

  const handleSubmit = async (
    values: UpdateChannelModel,
    _startNow: boolean
  ) => {
    await updateChannelMutation.mutateAsync({
      channelId,
      payload: values,
    })
  }

  return (
    <>
      <PageHeaderCard
        icon={<ChannelsIcon />}
        title={t("channels.editTitle")}
        description={t("channels.editDescription")}
        headerRight={
          <Button
            type="button"
            variant="outline"
            onClick={() => navigate({ to: "/channels" })}
          >
            <BackIcon data-icon="inline-start" />
            {t("channels.actions.backToChannels")}
          </Button>
        }
      />

      <SectionCard contentClassName="flex flex-col gap-6">
        {isLoading || !channel ? (
          <p className="text-sm text-muted-foreground">
            {t("channels.errors.loadChannel")}
          </p>
        ) : (
          <ChannelForm
            schema={updateChannelSchema}
            pending={updateChannelMutation.isPending}
            isEdit
            initialValues={{
              name: channel.name,
              kind: channel.kind,
              token: "",
              settingsJson: channel.settingsJson ?? "",
              agentId: channel.agentId ?? "",
              agentTeamId: channel.agentTeamId ?? "",
              startNow: false,
            }}
            onSubmit={handleSubmit}
          />
        )}
      </SectionCard>

      <SectionCard
        title="Chat History"
        description="Conversation history linked to this channel."
        headerRight={
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={isFetchingConversations}
            onClick={() => {
              void refetchConversations()
            }}
          >
            Refresh
          </Button>
        }
      >
        <div className="flex flex-col gap-6">
          {isConversationsError ? (
            <div className="rounded-xl border border-destructive/20 bg-destructive/5 px-4 py-3 text-sm text-destructive">
              Failed to load channel conversations.
            </div>
          ) : (
            <>
              <DataTable
                table={conversationsTable}
                loading={isLoadingConversations}
                emptyMessage="No conversation history for this channel yet."
              />
              <DataTablePagination
                table={conversationsTable}
                rowCount={conversations.length}
                isFetching={isFetchingConversations && !isLoadingConversations}
                pageSizeOptions={pageSizeOptions}
              />
            </>
          )}

          {selectedConversationId ? (
            <div className="rounded-xl border p-4">
              <div className="mb-4 flex items-start justify-between gap-4">
                <div>
                  <h3 className="text-sm font-semibold">
                    {selectedConversation?.title ||
                      selectedConversation?.externalId ||
                      "Conversation Detail"}
                  </h3>
                  <p className="text-sm text-muted-foreground">
                    {selectedConversation
                      ? `${selectedConversation.messages.length} messages • updated ${formatDateTime(
                          selectedConversation.lastMessageAt
                        )}`
                      : "Loading conversation detail..."}
                  </p>
                </div>
              </div>

              {isLoadingSelectedConversation ? (
                <p className="text-sm text-muted-foreground">
                  Loading messages...
                </p>
              ) : selectedConversation ? (
                <div className="flex max-h-[480px] flex-col gap-3 overflow-y-auto">
                  {selectedConversation.messages.map((message) => (
                    <div key={message.id} className="rounded-lg border p-3">
                      <div className="mb-2 flex items-center justify-between gap-3">
                        <span className="text-sm font-medium uppercase">
                          {message.role}
                        </span>
                        <span className="text-xs text-muted-foreground">
                          {formatDateTime(message.createdAt)}
                        </span>
                      </div>
                      <p className="whitespace-pre-wrap break-words text-sm">
                        {message.content || "(empty message)"}
                      </p>
                    </div>
                  ))}
                </div>
              ) : (
                <p className="text-sm text-muted-foreground">
                  Could not load conversation detail.
                </p>
              )}
            </div>
          ) : null}
        </div>
      </SectionCard>
    </>
  )
}
