import { z } from "zod"

import type { BaseRequestModel } from "./common"

export const createAgentSchema = z.object({
  name: z.string().min(1, "validation.required"),
  role: z.string().min(1, "validation.required"),
  kind: z.string().min(1, "validation.required"),
  type: z.string().min(1, "validation.required"),
  status: z
    .string()
    .refine(
      (value) => ["active", "paused", "archived"].includes(value),
      "validation.agentStatus"
    ),
  providerIds: z.array(z.string()).default([]),
  skillIds: z.array(z.string()).default([]),
  modelOverride: z.string().optional(),
  systemPrompt: z.string().min(1, "validation.required"),
  temperature: z.coerce.number().min(0).max(2).optional(),
  maxTokens: z.coerce.number().int().positive().optional(),
  metadataJson: z.string().optional(),
})

export type CreateAgentModel = z.infer<typeof createAgentSchema>

export const updateAgentSchema = createAgentSchema

export type UpdateAgentModel = z.infer<typeof updateAgentSchema>

export const agentProviderLinkSchema = z.object({
  providerId: z.string(),
  name: z.string(),
  provider: z.string(),
  model: z.string(),
  priority: z.number(),
})

export const agentSchema = z.object({
  id: z.string(),
  name: z.string(),
  role: z.string(),
  kind: z.string(),
  type: z.string(),
  status: z.string(),
  modelOverride: z.string().nullable().optional(),
  systemPrompt: z.string(),
  temperature: z.number().nullable().optional(),
  maxTokens: z.number().nullable().optional(),
  metadataJson: z.string().nullable().optional(),
  providers: z.array(agentProviderLinkSchema),
  providerIds: z.array(z.string()),
  skillIds: z.array(z.string()),
  createdAt: z.string(),
  updatedAt: z.string().nullable().optional(),
})

export type AgentModel = z.infer<typeof agentSchema>

export type GetAgentRequestModel = {
  status?: string
} & BaseRequestModel
