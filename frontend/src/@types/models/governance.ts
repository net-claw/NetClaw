import { z } from "zod"

export const governanceSettingSchema = z.object({
  id: z.string(),
  scope_type: z.string(),
  scope_id: z.string().nullable().optional(),
  enable_builtin_prompt_injection: z.boolean(),
  enable_custom_prompt_injection: z.boolean(),
  enable_audit: z.boolean(),
  enable_metrics: z.boolean(),
  enable_circuit_breaker: z.boolean(),
  builtin_detector_config: z.string().nullable().optional(),
  is_active: z.boolean(),
  created_on: z.string(),
  updated_on: z.string().nullable().optional(),
})

export type GovernanceSettingModel = z.infer<typeof governanceSettingSchema>

export const updateGovernanceSettingSchema = z.object({
  enable_builtin_prompt_injection: z.boolean().default(true),
  enable_custom_prompt_injection: z.boolean().default(true),
  enable_audit: z.boolean().default(true),
  enable_metrics: z.boolean().default(true),
  enable_circuit_breaker: z.boolean().default(false),
  builtin_detector_config: z.string().nullable().optional(),
  is_active: z.boolean().default(true),
})

export type UpdateGovernanceSettingModel = z.infer<
  typeof updateGovernanceSettingSchema
>
