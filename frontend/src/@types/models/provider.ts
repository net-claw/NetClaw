import { z } from "zod"

import type { BaseRequestModel } from "./common"

export const providerTypes = [
  "openai",
  "deepseek",
  "gemini",
  "mistral",
  "xai",
  "groq",
  "anthropic",
  "openrouter",
  "ollama",
] as const

export const createProviderSchema = z.object({
  name: z.string().min(1, "validation.required"),
  provider: z.enum(providerTypes),
  model: z.string().min(1, "validation.required"),
  apiKey: z.string().min(1, "validation.required"),
  baseUrl: z.string().optional(),
  active: z.boolean().default(true),
})

export type CreateProviderModel = z.infer<typeof createProviderSchema>

export const updateProviderSchema = createProviderSchema.extend({
  apiKey: z.string().optional(),
})

export type UpdateProviderModel = z.infer<typeof updateProviderSchema>

export const providerSchema = z.object({
  id: z.string(),
  name: z.string(),
  providerType: z.string(),
  defaultModel: z.string(),
  baseUrl: z.string().nullable().optional(),
  isActive: z.boolean(),
  createdBy: z.string(),
  createdOn: z.string(),
  updatedBy: z.string().nullable().optional(),
  updatedOn: z.string().nullable().optional(),
})

export type ProviderModel = z.infer<typeof providerSchema>

export type GetProviderRequestModel = {
  active?: boolean
} & BaseRequestModel
