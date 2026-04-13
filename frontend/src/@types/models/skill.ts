import { z } from "zod"

import type { BaseRequestModel } from "./common"

export const createSkillSchema = z.object({
  name: z.string().min(1, "validation.required"),
  slug: z.string().min(1, "validation.required"),
  description: z.string().min(1, "validation.required"),
  fileName: z.string().endsWith(".md", "validation.skillFileName"),
  content: z.string().min(1, "validation.required"),
  status: z
    .string()
    .refine(
      (value) => ["active", "paused", "archived"].includes(value),
      "validation.agentStatus"
    ),
  metadataJson: z.string().optional(),
})

export type CreateSkillModel = z.infer<typeof createSkillSchema>

export const updateSkillSchema = createSkillSchema

export type UpdateSkillModel = z.infer<typeof updateSkillSchema>

export const skillSchema = z.object({
  id: z.string(),
  name: z.string(),
  slug: z.string(),
  description: z.string(),
  fileName: z.string(),
  content: z.string(),
  status: z.string(),
  metadataJson: z.string().nullable().optional(),
  archiveFileName: z.string().nullable().optional(),
  createdAt: z.string(),
  updatedAt: z.string().nullable().optional(),
})

export type SkillModel = z.infer<typeof skillSchema>

export type GetSkillRequestModel = {
  status?: string
} & BaseRequestModel
