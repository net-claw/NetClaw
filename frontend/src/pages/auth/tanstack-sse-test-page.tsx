import { useCallback, useEffect, useMemo, useRef, useState } from "react"
import { useQueryClient } from "@tanstack/react-query"

import type { ConversationMessageModel } from "@/@types/models"
import { PageHeaderCard } from "@/components/share/cards/page-header-card"
import { SectionCard } from "@/components/share/cards/section-card"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Textarea } from "@/components/ui/textarea"
import { useGetAgentList } from "@/hooks/api/use-agent"
import { useGetAgentTeamList } from "@/hooks/api/use-agent-team"
import {
  CONVERSATION_QUERY_KEYS,
  useGetConversationById,
  useGetConversationList,
} from "@/hooks/api/use-conversation"
import { actionIcons, appIcons } from "@/lib/icons"
import { cn } from "@/lib/utils"

// ─── Types ────────────────────────────────────────────────────────────────────

type TargetType = "agent" | "team"

type ToolCall = {
  callId: string
  name: string
  args: unknown
  result?: unknown
}

type StreamMessage = {
  id: string
  role: "user" | "assistant"
  text: string
  toolCalls: ToolCall[]
}

type SseEvent =
  | { type: "text"; delta: string }
  | { type: "tool_start"; call_id: string; name: string; args: unknown }
  | { type: "tool_result"; call_id: string; result: unknown }
  | { type: "done"; finish_reason: string }
  | { type: "error"; message: string }

// ─── Helpers ──────────────────────────────────────────────────────────────────

function createThreadId() {
  return crypto.randomUUID().replaceAll("-", "")
}

function formatDateTime(value?: string | null) {
  if (!value) return "n/a"
  const parsed = new Date(value)
  return isNaN(parsed.getTime())
    ? value
    : parsed.toLocaleString(undefined, { dateStyle: "short", timeStyle: "short" })
}

function safeStringify(value: unknown) {
  if (value == null) return ""
  if (typeof value === "string") return value
  try {
    return JSON.stringify(value, null, 2)
  } catch {
    return String(value)
  }
}

function toInitialMessages(messages: ConversationMessageModel[]): StreamMessage[] {
  return messages.map((message) => ({
    id: message.externalMessageId || message.id,
    role: message.role === "assistant" ? "assistant" : "user",
    text: message.content ?? "",
    toolCalls: [],
  }))
}

// ─── Custom SSE hook ──────────────────────────────────────────────────────────

function useStreamChat({
  threadId,
  agentId,
  teamId,
  initialMessages,
}: {
  threadId: string
  agentId?: string
  teamId?: string
  initialMessages: StreamMessage[]
}) {
  const [messages, setMessages] = useState<StreamMessage[]>(initialMessages)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const abortRef = useRef<AbortController | null>(null)

  useEffect(() => {
    setMessages(initialMessages)
  }, [initialMessages])

  const sendMessage = useCallback(
    async (text: string) => {
      if (isLoading) return

      const assistantMsgId = crypto.randomUUID()

      setMessages((prev) => [
        ...prev,
        { id: crypto.randomUUID(), role: "user", text, toolCalls: [] },
        { id: assistantMsgId, role: "assistant", text: "", toolCalls: [] },
      ])
      setIsLoading(true)
      setError(null)

      const abort = new AbortController()
      abortRef.current = abort

      try {
        const response = await fetch("/api/v1/chat/stream", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            threadId,
            message: text,
            agentId: agentId ?? null,
            teamId: teamId ?? null,
          }),
          signal: abort.signal,
        })

        if (!response.ok) {
          throw new Error(`Stream failed with status ${response.status}`)
        }

        const reader = response.body!.getReader()
        const decoder = new TextDecoder()
        let buffer = ""

        while (true) {
          const { done, value } = await reader.read()
          if (done) break

          buffer += decoder.decode(value, { stream: true })
          const lines = buffer.split("\n")
          buffer = lines.pop() ?? ""

          for (const line of lines) {
            if (!line.startsWith("data: ")) continue
            const json = line.slice(6).trim()
            if (!json) continue

            const event = JSON.parse(json) as SseEvent

            if (event.type === "error") {
              setError(event.message)
              break
            }

            if (event.type === "done") break

            setMessages((prev) => {
              const last = prev[prev.length - 1]
              if (!last || last.id !== assistantMsgId) return prev

              if (event.type === "text") {
                return [...prev.slice(0, -1), { ...last, text: last.text + event.delta }]
              }

              if (event.type === "tool_start") {
                return [
                  ...prev.slice(0, -1),
                  {
                    ...last,
                    toolCalls: [
                      ...last.toolCalls,
                      { callId: event.call_id, name: event.name, args: event.args },
                    ],
                  },
                ]
              }

              if (event.type === "tool_result") {
                return [
                  ...prev.slice(0, -1),
                  {
                    ...last,
                    toolCalls: last.toolCalls.map((tc) =>
                      tc.callId === event.call_id ? { ...tc, result: event.result } : tc
                    ),
                  },
                ]
              }

              return prev
            })
          }
        }
      } catch (err) {
        if (err instanceof Error && err.name !== "AbortError") {
          setError(err.message)
        }
      } finally {
        setIsLoading(false)
      }
    },
    [isLoading, threadId, agentId, teamId]
  )

  const stop = useCallback(() => {
    abortRef.current?.abort()
  }, [])

  const clear = useCallback(() => {
    setMessages([])
    setError(null)
  }, [])

  return { messages, sendMessage, stop, clear, isLoading, error }
}

// ─── ChatMessageCard ──────────────────────────────────────────────────────────

function ChatMessageCard({ message }: { message: StreamMessage }) {
  return (
    <div
      className={cn(
        "max-w-[88%] rounded-2xl px-4 py-3 text-sm",
        message.role === "user" ? "ml-auto bg-primary text-primary-foreground" : "bg-background"
      )}
    >
      <p className="mb-2 text-[11px] font-medium uppercase tracking-[0.18em] opacity-70">
        {message.role}
      </p>

      {message.text ? (
        <p className="whitespace-pre-wrap leading-6">{message.text}</p>
      ) : null}

      {message.toolCalls.length > 0 ? (
        <div className="mt-3 space-y-2">
          {message.toolCalls.map((tc) => (
            <div
              key={tc.callId}
              className={cn(
                "rounded-xl border px-3 py-2",
                message.role === "user"
                  ? "border-primary-foreground/20 bg-primary-foreground/10"
                  : "border-border bg-muted/20"
              )}
            >
              <div className="flex flex-wrap items-center gap-2">
                <Badge variant="outline">{tc.name}</Badge>
                <Badge variant="secondary">{tc.result !== undefined ? "done" : "running"}</Badge>
              </div>
              {tc.args ? (
                <pre className="mt-2 whitespace-pre-wrap break-all font-mono text-xs leading-5 opacity-80">
                  {safeStringify(tc.args)}
                </pre>
              ) : null}
            </div>
          ))}
        </div>
      ) : null}

      {!message.text && message.toolCalls.length === 0 ? (
        <p className="leading-6 opacity-70">Streaming...</p>
      ) : null}
    </div>
  )
}

// ─── ToolCallsPanel ───────────────────────────────────────────────────────────

function ToolCallsPanel({ messages }: { messages: StreamMessage[] }) {
  const toolEntries = useMemo(
    () =>
      messages.flatMap((msg) =>
        msg.toolCalls.map((tc) => ({ messageId: msg.id, tc }))
      ),
    [messages]
  )

  return (
    <Card className="border-dashed bg-muted/10">
      <CardHeader>
        <CardTitle>Tool Stream</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        {toolEntries.length === 0 ? (
          <p className="text-sm text-muted-foreground">
            No tool call has been captured yet. Ask the model to use a tool.
          </p>
        ) : (
          toolEntries.map(({ messageId, tc }) => (
            <div key={tc.callId} className="rounded-lg border bg-background p-3">
              <div className="flex flex-wrap items-center gap-2">
                <p className="font-medium text-foreground">{tc.name}</p>
                <Badge variant="outline" className="capitalize">
                  {tc.result !== undefined ? "done" : "running"}
                </Badge>
              </div>

              <p className="mt-2 text-[11px] text-muted-foreground">
                message: {messageId} · callId: {tc.callId}
              </p>

              {tc.args ? (
                <div className="mt-3">
                  <p className="text-xs font-medium text-foreground">Arguments</p>
                  <pre className="mt-1 whitespace-pre-wrap break-all rounded-lg bg-muted/20 p-3 font-mono text-xs leading-5 text-muted-foreground">
                    {safeStringify(tc.args)}
                  </pre>
                </div>
              ) : null}

              {tc.result !== undefined ? (
                <div className="mt-3">
                  <p className="text-xs font-medium text-foreground">Result</p>
                  <pre className="mt-1 whitespace-pre-wrap break-all rounded-lg bg-muted/20 p-3 font-mono text-xs leading-5 text-muted-foreground">
                    {safeStringify(tc.result)}
                  </pre>
                </div>
              ) : null}
            </div>
          ))
        )}
      </CardContent>
    </Card>
  )
}

// ─── TanstackChatSurface ──────────────────────────────────────────────────────

function TanstackChatSurface({
  targetType,
  targetId,
  activeThreadId,
  initialMessages,
  onSettled,
}: {
  targetType: TargetType
  targetId: string
  activeThreadId: string
  initialMessages: StreamMessage[]
  onSettled: () => void
}) {
  const SendIcon = actionIcons.create
  const StopIcon = actionIcons.stop
  const [prompt, setPrompt] = useState("")

  const { messages, sendMessage, stop, clear, isLoading, error } = useStreamChat({
    threadId: activeThreadId,
    agentId: targetType === "agent" ? targetId : undefined,
    teamId: targetType === "team" ? targetId : undefined,
    initialMessages,
  })

  useEffect(() => {
    if (!isLoading && messages.length > initialMessages.length) {
      const timeoutId = window.setTimeout(() => {
        onSettled()
      }, 1200)
      return () => window.clearTimeout(timeoutId)
    }
  }, [initialMessages.length, isLoading, messages.length, onSettled])

  const handleSend = async () => {
    const trimmedPrompt = prompt.trim()
    if (!trimmedPrompt || isLoading) return
    await sendMessage(trimmedPrompt)
    setPrompt("")
  }

  return (
    <>
      <Card className="flex min-h-0 flex-col">
        <CardHeader>
          <CardTitle>Chat</CardTitle>
        </CardHeader>
        <CardContent className="flex min-h-0 flex-1 flex-col gap-4">
          <div className="grid gap-3 rounded-xl border bg-muted/20 p-4 text-sm sm:grid-cols-3">
            <div>
              <p className="font-medium text-foreground">Status</p>
              <Badge variant="secondary" className="mt-2 capitalize">
                {isLoading ? "streaming" : "idle"}
              </Badge>
            </div>
            <div>
              <p className="font-medium text-foreground">Thread</p>
              <p className="mt-2 font-mono text-[11px] text-muted-foreground">{activeThreadId}</p>
            </div>
            <div>
              <p className="font-medium text-foreground">Messages</p>
              <p className="mt-2 text-muted-foreground">{messages.length}</p>
            </div>
          </div>

          <div className="min-h-0 flex-1 space-y-3 overflow-y-auto rounded-xl border bg-muted/10 p-4">
            {messages.length === 0 ? (
              <div className="rounded-lg border border-dashed p-4 text-sm text-muted-foreground">
                Send a prompt to start streaming via <code>/api/v1/chat/stream</code>.
              </div>
            ) : (
              messages.map((message) => (
                <ChatMessageCard key={message.id} message={message} />
              ))
            )}
          </div>

          <div className="space-y-3">
            <Textarea
              value={prompt}
              onChange={(event) => setPrompt(event.target.value)}
              placeholder="Type a test prompt..."
              rows={4}
              disabled={isLoading}
            />
            <div className="flex items-center justify-between gap-3">
              <p className="text-xs text-muted-foreground">
                Transport: <code>POST /api/v1/chat/stream</code> · custom SSE
              </p>
              <div className="flex items-center gap-2">
                <Button type="button" variant="outline" onClick={clear}>
                  Clear
                </Button>
                {isLoading ? (
                  <Button type="button" variant="outline" onClick={stop}>
                    <StopIcon data-icon="inline-start" />
                    Stop
                  </Button>
                ) : null}
                <Button type="button" onClick={() => void handleSend()} disabled={isLoading}>
                  <SendIcon data-icon="inline-start" />
                  {isLoading ? "Streaming..." : "Send"}
                </Button>
              </div>
            </div>
            {error ? <p className="text-sm text-destructive">{error}</p> : null}
          </div>
        </CardContent>
      </Card>

      <div className="min-w-0">
        <ToolCallsPanel messages={messages} />
      </div>
    </>
  )
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function TanstackSseTestPage() {
  const AgentIcon = appIcons.channels
  const RefreshIcon = actionIcons.refresh
  const queryClient = useQueryClient()
  const [targetType, setTargetType] = useState<TargetType>("agent")
  const [selectedAgentId, setSelectedAgentId] = useState("")
  const [selectedTeamId, setSelectedTeamId] = useState("")
  const [selectedConversationId, setSelectedConversationId] = useState("")
  const [draftThreadId, setDraftThreadId] = useState(() => createThreadId())

  const { data, isLoading, isError } = useGetAgentList({
    pageIndex: 0,
    pageSize: 100,
    ascending: true,
    orderBy: "name",
  })
  const {
    data: teamsData,
    isLoading: isLoadingTeams,
    isError: isErrorTeams,
  } = useGetAgentTeamList({
    pageIndex: 0,
    pageSize: 100,
    ascending: true,
    orderBy: "name",
    status: "active",
  })

  const agents = data?.items ?? []
  const teams = teamsData?.items ?? []
  const targetId = targetType === "team" ? selectedTeamId : selectedAgentId
  const selectedAgent = agents.find((agent) => agent.id === selectedAgentId)
  const selectedTeam = teams.find((team) => team.id === selectedTeamId)
  const { data: conversations = [], refetch: refetchConversations } = useGetConversationList(
    { targetType, targetId },
    Boolean(targetId)
  )
  const { data: selectedConversation } = useGetConversationById(
    selectedConversationId,
    Boolean(selectedConversationId)
  )

  const activeThreadId = selectedConversation?.externalId || draftThreadId
  const initialMessages = useMemo(
    () => toInitialMessages(selectedConversation?.messages ?? []),
    [selectedConversation?.messages]
  )

  useEffect(() => {
    if (!selectedAgentId && agents.length > 0) setSelectedAgentId(agents[0].id)
  }, [agents, selectedAgentId])

  useEffect(() => {
    if (!selectedTeamId && teams.length > 0) setSelectedTeamId(teams[0].id)
  }, [selectedTeamId, teams])

  useEffect(() => {
    setSelectedConversationId("")
    setDraftThreadId(createThreadId())
  }, [selectedAgentId, selectedTeamId, targetType])

  const handleRefreshConversationHistory = useCallback(() => {
    void queryClient.invalidateQueries({
      queryKey: CONVERSATION_QUERY_KEYS.useGetConversationList({ targetType, targetId }),
    })
    void refetchConversations()
  }, [queryClient, refetchConversations, targetId, targetType])

  const handleNewChat = () => {
    setSelectedConversationId("")
    setDraftThreadId(createThreadId())
  }

  return (
    <div className="flex flex-col gap-6">
      <PageHeaderCard
        icon={<AgentIcon />}
        title="Custom SSE Test"
        description="Chat over a custom SSE stream endpoint with full tool call visibility."
        headerRight={
          <Button type="button" variant="outline" onClick={handleNewChat}>
            <RefreshIcon data-icon="inline-start" />
            New chat
          </Button>
        }
      />

      <SectionCard
        title="Connection"
        description="Select a target agent or team. Chat streams via POST /api/v1/chat/stream with custom SSE events."
      >
        <div className="grid gap-4 rounded-xl border bg-muted/20 p-4 text-sm leading-6 text-muted-foreground lg:grid-cols-3">
          <div className="flex flex-col gap-2 lg:col-span-3">
            <p className="text-sm font-medium text-foreground">Target</p>
            <Tabs
              value={targetType}
              onValueChange={(value) => setTargetType(value as TargetType)}
            >
              <TabsList>
                <TabsTrigger value="agent">Direct Agent</TabsTrigger>
                <TabsTrigger value="team">Agent Team</TabsTrigger>
              </TabsList>
            </Tabs>
          </div>

          <div className="flex flex-col gap-2">
            <p className="text-sm font-medium text-foreground">
              {targetType === "team" ? "Agent Team" : "Agent"}
            </p>
            {targetType === "team" ? (
              <Select
                value={selectedTeamId}
                onValueChange={setSelectedTeamId}
                disabled={isLoadingTeams || teams.length === 0}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Select team" />
                </SelectTrigger>
                <SelectContent>
                  {teams.map((item) => (
                    <SelectItem key={item.id} value={item.id}>
                      {item.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            ) : (
              <Select
                value={selectedAgentId}
                onValueChange={setSelectedAgentId}
                disabled={isLoading || agents.length === 0}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Select agent" />
                </SelectTrigger>
                <SelectContent>
                  {agents.map((item) => (
                    <SelectItem key={item.id} value={item.id}>
                      {item.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}
          </div>

          <div className="rounded-lg border bg-background p-3">
            <p className="text-sm font-medium text-foreground">
              {targetType === "team" ? "Team members" : "Provider chain"}
            </p>
            {targetType === "team" ? (
              selectedTeam?.members.length ? (
                <div className="mt-2 space-y-1">
                  {selectedTeam.members
                    .slice()
                    .sort((a, b) => a.order - b.order)
                    .map((member) => (
                      <p key={member.id}>
                        {member.order + 1}. {member.agentName || member.agentId}
                        {member.role ? ` - ${member.role}` : ""}
                        {member.reportsToMemberId
                          ? ` -> ${member.reportsToMemberName || member.reportsToMemberId}`
                          : " [root]"}
                      </p>
                    ))}
                </div>
              ) : (
                <p className="mt-2">No members configured for this team.</p>
              )
            ) : selectedAgent?.providers.length ? (
              <div className="mt-2 space-y-1">
                {selectedAgent.providers.map((provider) => (
                  <p key={provider.providerId}>
                    {provider.priority + 1}. {provider.name} ({provider.model})
                  </p>
                ))}
              </div>
            ) : (
              <p className="mt-2">No provider configured for this agent.</p>
            )}
          </div>

          <div className="space-y-1">
            <p>
              Endpoint: <code>/api/v1/chat/stream</code>
            </p>
            <p>
              Thread: <code className="text-[11px]">{activeThreadId}</code>
            </p>
          </div>

          {targetType === "agent" && isError ? (
            <p className="text-sm text-destructive lg:col-span-3">Could not load agents.</p>
          ) : null}
          {targetType === "team" && isErrorTeams ? (
            <p className="text-sm text-destructive lg:col-span-3">Could not load agent teams.</p>
          ) : null}
        </div>
      </SectionCard>

      <SectionCard className="overflow-hidden p-0">
        <div className="grid min-h-[640px] gap-6 p-6 xl:grid-cols-[280px_minmax(0,1fr)_360px]">
          <div className="min-h-0">
            <Card className="flex h-full flex-col">
              <CardHeader className="flex flex-row items-center justify-between space-y-0">
                <div>
                  <CardTitle>Conversations</CardTitle>
                  <p className="text-sm text-muted-foreground">
                    {targetType === "team"
                      ? "History for the selected team."
                      : "History for the selected agent."}
                  </p>
                </div>
                <Button type="button" size="sm" variant="outline" onClick={handleNewChat}>
                  New chat
                </Button>
              </CardHeader>
              <CardContent className="flex min-h-0 flex-1 flex-col gap-3 overflow-hidden">
                <div className="rounded-lg border bg-muted/20 p-3 text-xs text-muted-foreground">
                  <p>
                    Active thread:{" "}
                    <span className="font-mono text-[11px] text-foreground">{activeThreadId}</span>
                  </p>
                  <p className="mt-1">
                    {selectedConversation
                      ? "Continuing a saved conversation."
                      : "New messages will create or extend this thread."}
                  </p>
                </div>

                <div className="min-h-0 flex-1 space-y-2 overflow-y-auto pr-1">
                  {conversations.length === 0 ? (
                    <div className="rounded-lg border border-dashed p-4 text-sm text-muted-foreground">
                      No saved conversations for this target yet.
                    </div>
                  ) : (
                    conversations.map((conversation) => (
                      <button
                        key={conversation.id}
                        type="button"
                        onClick={() => setSelectedConversationId(conversation.id)}
                        className={cn(
                          "w-full rounded-xl border p-3 text-left transition-colors",
                          selectedConversationId === conversation.id
                            ? "border-primary/40 bg-primary/5"
                            : "bg-background hover:bg-muted/40"
                        )}
                      >
                        <div className="flex items-center justify-between gap-3">
                          <p className="line-clamp-1 text-sm font-medium text-foreground">
                            {conversation.title || "Untitled conversation"}
                          </p>
                          <Badge variant="outline" className="capitalize">
                            {conversation.status}
                          </Badge>
                        </div>
                        <p className="mt-2 text-xs text-muted-foreground">
                          {formatDateTime(conversation.lastMessageAt)}
                        </p>
                        <p className="mt-1 line-clamp-2 font-mono text-[11px] text-muted-foreground">
                          {conversation.externalId}
                        </p>
                      </button>
                    ))
                  )}
                </div>
              </CardContent>
            </Card>
          </div>

          {targetId ? (
            <TanstackChatSurface
              key={`${targetType}-${targetId}-${activeThreadId}`}
              targetType={targetType}
              targetId={targetId}
              activeThreadId={activeThreadId}
              initialMessages={initialMessages}
              onSettled={handleRefreshConversationHistory}
            />
          ) : (
            <Card className="xl:col-span-2">
              <CardHeader>
                <CardTitle>Chat</CardTitle>
              </CardHeader>
              <CardContent className="text-sm text-muted-foreground">
                Select an agent or team to start the SSE chat.
              </CardContent>
            </Card>
          )}
        </div>
      </SectionCard>
    </div>
  )
}
