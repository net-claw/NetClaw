import { z } from "zod"

export const governanceSettingSchema = z.object({
  id: z.string(),
  scopeType: z.string(),
  scopeId: z.string().nullable().optional(),
  enableBuiltinPromptInjection: z.boolean(),
  enableCustomPromptInjection: z.boolean(),
  enableAudit: z.boolean(),
  enableMetrics: z.boolean(),
  enableCircuitBreaker: z.boolean(),
  builtinDetectorConfig: z.string().nullable().optional(),
  isActive: z.boolean(),
  createdOn: z.string(),
  updatedOn: z.string().nullable().optional(),
})

export type GovernanceSettingModel = z.infer<typeof governanceSettingSchema>

export const updateGovernanceSettingSchema = z.object({
  enableBuiltinPromptInjection: z.boolean().default(true),
  enableCustomPromptInjection: z.boolean().default(true),
  enableAudit: z.boolean().default(true),
  enableMetrics: z.boolean().default(true),
  enableCircuitBreaker: z.boolean().default(false),
  builtinDetectorConfig: z.string().nullable().optional(),
  isActive: z.boolean().default(true),
})

export type UpdateGovernanceSettingModel = z.infer<
  typeof updateGovernanceSettingSchema
>
