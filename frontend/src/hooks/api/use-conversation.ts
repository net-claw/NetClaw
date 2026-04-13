import { useQuery } from "@tanstack/react-query"

import type { ConversationListRequestModel } from "@/@types/models"
import { conversationService } from "@/services/api/conversation-service"

export const CONVERSATION_QUERY_KEYS = {
  useGetConversationList: (params?: ConversationListRequestModel) => [
    "useGetConversationList",
    ...(params ? Object.values(params) : []),
  ],
  useGetConversationById: (conversationId: string) => [
    "useGetConversationById",
    conversationId,
  ],
}

export const useGetConversationList = (
  params: ConversationListRequestModel,
  enabled = true
) =>
  useQuery({
    queryKey: CONVERSATION_QUERY_KEYS.useGetConversationList(params),
    queryFn: () => conversationService.getList(params),
    enabled: enabled && Boolean(params.targetId || params.externalId),
  })

export const useGetConversationById = (
  conversationId: string,
  enabled = true
) =>
  useQuery({
    queryKey: CONVERSATION_QUERY_KEYS.useGetConversationById(conversationId),
    queryFn: () => conversationService.getById(conversationId),
    enabled: enabled && Boolean(conversationId),
  })
