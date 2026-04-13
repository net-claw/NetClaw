import { zodResolver } from "@hookform/resolvers/zod"
import { useEffect } from "react"
import {
  Controller,
  type Control,
  type Resolver,
  useForm,
} from "react-hook-form"
import { useTranslation } from "react-i18next"

import {
  type UpdateGovernanceSettingModel,
  updateGovernanceSettingSchema,
} from "@/@types/models"
import { PageHeaderCard } from "@/components/share/cards/page-header-card"
import { SectionCard } from "@/components/share/cards/section-card"
import { ControlledField } from "@/components/form/controlled-field"
import { Button } from "@/components/ui/button"
import { Checkbox } from "@/components/ui/checkbox"
import {
  Field,
  FieldContent,
  FieldDescription,
  FieldGroup,
  FieldLabel,
} from "@/components/ui/field"
import {
  useGetGlobalGovernanceSetting,
  useUpdateGlobalGovernanceSetting,
} from "@/hooks/api/use-governance"
import { appIcons } from "@/lib/icons"

const emptyValues: UpdateGovernanceSettingModel = {
  enableBuiltinPromptInjection: true,
  enableCustomPromptInjection: true,
  enableAudit: true,
  enableMetrics: true,
  enableCircuitBreaker: false,
  builtinDetectorConfig: null,
  isActive: true,
}

export default function GovernancePage() {
  const { t } = useTranslation()
  const GovernanceIcon = appIcons.settings
  const { data, isLoading } = useGetGlobalGovernanceSetting()
  const updateMutation = useUpdateGlobalGovernanceSetting()

  const form = useForm<UpdateGovernanceSettingModel>({
    resolver: zodResolver(
      updateGovernanceSettingSchema
    ) as unknown as Resolver<UpdateGovernanceSettingModel>,
    defaultValues: emptyValues,
  })

  useEffect(() => {
    if (!data) {
      return
    }

    form.reset({
      enableBuiltinPromptInjection: data.enableBuiltinPromptInjection,
      enableCustomPromptInjection: data.enableCustomPromptInjection,
      enableAudit: data.enableAudit,
      enableMetrics: data.enableMetrics,
      enableCircuitBreaker: data.enableCircuitBreaker,
      builtinDetectorConfig: data.builtinDetectorConfig ?? null,
      isActive: data.isActive,
    })
  }, [data, form])

  const handleSubmit = async (values: UpdateGovernanceSettingModel) => {
    await updateMutation.mutateAsync(values)
  }

  return (
    <>
      <PageHeaderCard
        icon={<GovernanceIcon />}
        title={t("governance.page.title")}
        description={t("governance.page.description")}
      />

      <SectionCard contentClassName="flex flex-col gap-6">
        {isLoading || !data ? (
          <p className="text-sm text-muted-foreground">
            {t("governance.loading")}
          </p>
        ) : (
          <form
            className="flex flex-col gap-6"
            onSubmit={form.handleSubmit((values) => handleSubmit(values))}
          >
            <FieldGroup>
              <SettingCheckbox
                control={form.control}
                name="isActive"
                label={t("governance.form.isActive")}
                description={t("governance.form.isActiveHelp")}
              />

              <SettingCheckbox
                control={form.control}
                name="enableBuiltinPromptInjection"
                label={t("governance.form.enableBuiltinPromptInjection")}
                description={t(
                  "governance.form.enableBuiltinPromptInjectionHelp"
                )}
              />

              <SettingCheckbox
                control={form.control}
                name="enableCustomPromptInjection"
                label={t("governance.form.enableCustomPromptInjection")}
                description={t(
                  "governance.form.enableCustomPromptInjectionHelp"
                )}
              />

              <SettingCheckbox
                control={form.control}
                name="enableAudit"
                label={t("governance.form.enableAudit")}
                description={t("governance.form.enableAuditHelp")}
              />

              <SettingCheckbox
                control={form.control}
                name="enableMetrics"
                label={t("governance.form.enableMetrics")}
                description={t("governance.form.enableMetricsHelp")}
              />

              <SettingCheckbox
                control={form.control}
                name="enableCircuitBreaker"
                label={t("governance.form.enableCircuitBreaker")}
                description={t("governance.form.enableCircuitBreakerHelp")}
              />

              <ControlledField
                name="builtinDetectorConfig"
                control={form.control}
                label={t("governance.form.builtinDetectorConfig")}
                placeholder={t("governance.form.builtinDetectorConfigPlaceholder")}
                description={t("governance.form.builtinDetectorConfigHelp")}
              />
            </FieldGroup>

            <div className="flex justify-end">
              <Button type="submit" disabled={updateMutation.isPending}>
                {updateMutation.isPending
                  ? t("governance.actions.saving")
                  : t("governance.actions.save")}
              </Button>
            </div>
          </form>
        )}
      </SectionCard>
    </>
  )
}

type SettingCheckboxProps = {
  control: Control<UpdateGovernanceSettingModel>
  name:
    | "isActive"
    | "enableBuiltinPromptInjection"
    | "enableCustomPromptInjection"
    | "enableAudit"
    | "enableMetrics"
    | "enableCircuitBreaker"
  label: string
  description: string
}

function SettingCheckbox({
  control,
  name,
  label,
  description,
}: SettingCheckboxProps) {
  return (
    <Controller
      name={name}
      control={control}
      render={({ field }) => (
        <Field orientation="horizontal">
          <Checkbox
            checked={field.value}
            onCheckedChange={(checked) => field.onChange(Boolean(checked))}
          />
          <FieldContent>
            <FieldLabel>{label}</FieldLabel>
            <FieldDescription>{description}</FieldDescription>
          </FieldContent>
        </Field>
      )}
    />
  )
}
