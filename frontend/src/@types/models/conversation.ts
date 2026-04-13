export type ConversationListRequestModel = {
  externalId?: string
  targetType?: string
  targetId?: string
}

export type ConversationMessageModel = {
  id: string
  sequence: number
  role: string
  content?: string | null
  externalMessageId?: string | null
  metadataJson?: string | null
  createdAt: string
}

export type ConversationListItemModel = {
  id: string
  externalId: string
  title?: string | null
  status: string
  targetType?: string | null
  targetId?: string | null
  metadataJson?: string | null
  lastMessageAt: string
  createdAt: string
}

export type ConversationModel = {
  id: string
  externalId: string
  title?: string | null
  status: string
  targetType?: string | null
  targetId?: string | null
  metadataJson?: string | null
  lastMessageAt: string
  createdAt: string
  messages: ConversationMessageModel[]
}
