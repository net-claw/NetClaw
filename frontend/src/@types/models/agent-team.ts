import { z } from "zod"

import type { BaseRequestModel } from "./common"

const teamStatusSchema = z
  .string()
  .refine(
    (value) => ["active", "paused", "archived"].includes(value),
    "validation.agentStatus"
  )

export const agentTeamMemberSchema = z.object({
  id: z.string().optional(),
  agentId: z.string().min(1, "validation.required"),
  role: z.string().optional(),
  order: z.coerce.number().int().min(0),
  status: teamStatusSchema,
  reportsToMemberId: z.string().optional(),
  metadataJson: z.string().optional(),
})

export const createAgentTeamSchema = z.object({
  name: z.string().min(1, "validation.required"),
  description: z.string().optional(),
  status: teamStatusSchema,
  metadataJson: z.string().optional(),
  members: z.array(agentTeamMemberSchema).min(1, "validation.required"),
})

export type CreateAgentTeamModel = z.infer<typeof createAgentTeamSchema>

export const updateAgentTeamSchema = createAgentTeamSchema

export type UpdateAgentTeamModel = z.infer<typeof updateAgentTeamSchema>

export const agentTeamMemberResponseSchema = z.object({
  id: z.string(),
  agentId: z.string(),
  agentName: z.string().nullable().optional(),
  role: z.string().nullable().optional(),
  order: z.number(),
  status: z.string(),
  reportsToMemberId: z.string().nullable().optional(),
  reportsToMemberName: z.string().nullable().optional(),
  metadataJson: z.string().nullable().optional(),
  createdAt: z.string(),
  updatedAt: z.string().nullable().optional(),
})

export const agentTeamSchema = z.object({
  id: z.string(),
  name: z.string(),
  description: z.string().nullable().optional(),
  status: z.string(),
  metadataJson: z.string().nullable().optional(),
  members: z.array(agentTeamMemberResponseSchema),
  createdAt: z.string(),
  updatedAt: z.string().nullable().optional(),
})

export type AgentTeamModel = z.infer<typeof agentTeamSchema>

export type GetAgentTeamRequestModel = {
  status?: string
} & BaseRequestModel
