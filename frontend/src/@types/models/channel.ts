import { z } from "zod"

import type { BaseRequestModel } from "./common"

export const channelKinds = [
  "discord",
  "telegram",
  "whatsapp",
  "slack",
  "web",
] as const

export const channelStatuses = [
  "stopped",
  "starting",
  "running",
  "stopping",
  "error",
] as const

export const createChannelSchema = z.object({
  name: z.string().min(1, "validation.required"),
  kind: z.enum(channelKinds),
  token: z.string().min(1, "validation.required"),
  settingsJson: z.string().optional(),
  startNow: z.boolean().default(false),
})

export type CreateChannelModel = z.infer<typeof createChannelSchema>

export const updateChannelSchema = createChannelSchema.extend({
  token: z.string().optional(),
})

export type UpdateChannelModel = z.infer<typeof updateChannelSchema>

export const channelSchema = z.object({
  id: z.string(),
  name: z.string(),
  kind: z.enum(channelKinds),
  status: z.enum(channelStatuses),
  settingsJson: z.string().nullable().optional(),
  hasCredentials: z.boolean(),
  createdBy: z.string(),
  createdOn: z.string(),
  updatedBy: z.string().nullable().optional(),
  updatedOn: z.string().nullable().optional(),
  deletedAt: z.string().nullable().optional(),
})

export type ChannelModel = z.infer<typeof channelSchema>

export type GetChannelRequestModel = {
  kind?: string
  status?: string
} & BaseRequestModel
