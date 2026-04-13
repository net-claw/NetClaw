import { z } from "zod"

import type { BaseRequestModel } from "./common"

export const createSkillSchema = z.object({
  name: z.string().min(1, "validation.required"),
  slug: z.string().min(1, "validation.required"),
  description: z.string().min(1, "validation.required"),
  file_name: z.string().endsWith(".md", "validation.skillFileName"),
  content: z.string().min(1, "validation.required"),
  status: z
    .string()
    .refine(
      (value) => ["active", "paused", "archived"].includes(value),
      "validation.agentStatus"
    ),
  metadata_json: z.string().optional(),
})

export type CreateSkillModel = z.infer<typeof createSkillSchema>

export const updateSkillSchema = createSkillSchema

export type UpdateSkillModel = z.infer<typeof updateSkillSchema>

export const skillSchema = z.object({
  id: z.string(),
  name: z.string(),
  slug: z.string(),
  description: z.string(),
  file_name: z.string(),
  content: z.string(),
  status: z.string(),
  metadata_json: z.string().nullable().optional(),
  archive_file_name: z.string().nullable().optional(),
  created_at: z.string(),
  updated_at: z.string().nullable().optional(),
})

export type SkillModel = z.infer<typeof skillSchema>

export type GetSkillRequestModel = {
  status?: string
} & BaseRequestModel
