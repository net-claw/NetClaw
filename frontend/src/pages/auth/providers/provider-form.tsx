import { zodResolver } from "@hookform/resolvers/zod"
import { useEffect } from "react"
import { Controller, type Resolver, useForm } from "react-hook-form"

import type { CreateProviderModel } from "@/@types/models"
import { ControlledField } from "@/components/form/controlled-field"
import { Button } from "@/components/ui/button"
import {
  Field,
  FieldDescription,
  FieldGroup,
  FieldLabel,
} from "@/components/ui/field"
import { Checkbox } from "@/components/ui/checkbox"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { useTranslation } from "react-i18next"
import type { z } from "zod"

type ProviderFormValues = CreateProviderModel

type ProviderFormProps = {
  initialValues?: Partial<ProviderFormValues>
  pending?: boolean
  schema: z.ZodTypeAny
  isEdit?: boolean
  onSubmit: (values: ProviderFormValues) => void
}

const emptyValues: ProviderFormValues = {
  name: "",
  provider: "openai",
  model: "",
  apiKey: "",
  baseUrl: "",
  active: true,
}

export function ProviderForm({
  initialValues,
  pending = false,
  schema,
  isEdit = false,
  onSubmit,
}: ProviderFormProps) {
  const { t } = useTranslation()
  const form = useForm<ProviderFormValues>({
    resolver: zodResolver(
      schema as never
    ) as unknown as Resolver<ProviderFormValues>,
    defaultValues: {
      ...emptyValues,
      ...initialValues,
    },
  })

  useEffect(() => {
    form.reset({
      ...emptyValues,
      ...initialValues,
    })
  }, [form, initialValues])

  const selectedProvider = form.watch("provider")

  const apiKeyPlaceholder =
    selectedProvider === "openai"
      ? t("providers.form.apiKeyPlaceholder")
      : t("providers.form.apiKeyPlaceholder")

  return (
    <form
      className="flex flex-col gap-6"
      onSubmit={form.handleSubmit((values) => onSubmit(values))}
    >
      <FieldGroup>
        <div className="grid gap-5 md:grid-cols-2">
          <ControlledField
            name="name"
            control={form.control}
            label={t("providers.form.name")}
            placeholder={t("providers.form.namePlaceholder")}
          />

          <Controller
            name="provider"
            control={form.control}
            render={({ field }) => (
              <Field>
                <FieldLabel>{t("providers.form.provider")}</FieldLabel>
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger>
                    <SelectValue
                      placeholder={t("providers.form.selectProvider")}
                    />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="openai">ChatGPT / OpenAI</SelectItem>
                    <SelectItem value="deepseek">DeepSeek</SelectItem>
                    <SelectItem value="gemini">Gemini</SelectItem>
                  </SelectContent>
                </Select>
              </Field>
            )}
          />
        </div>

        <div className="grid gap-5 md:grid-cols-2">
          <ControlledField
            name="model"
            control={form.control}
            label={t("providers.form.model")}
            placeholder={t("providers.form.modelPlaceholder")}
          />

          <ControlledField
            name="baseUrl"
            control={form.control}
            label={t("providers.form.baseUrl")}
            placeholder={t("providers.form.baseUrlPlaceholder")}
          />
        </div>

        <ControlledField
          name="apiKey"
          control={form.control}
          label={t("providers.form.apiKey")}
          placeholder={apiKeyPlaceholder}
          description={
            isEdit
              ? t("providers.form.apiKeyUpdateHelp")
              : t("providers.form.apiKeyHelp")
          }
        />

        <Controller
          name="active"
          control={form.control}
          render={({ field }) => (
            <Field orientation="horizontal">
              <Checkbox
                checked={field.value}
                onCheckedChange={(checked) => field.onChange(Boolean(checked))}
              />
              <div className="flex flex-col gap-1">
                <FieldLabel>{t("providers.form.active")}</FieldLabel>
                <FieldDescription>
                  {t("providers.form.activeHelp")}
                </FieldDescription>
              </div>
            </Field>
          )}
        />
      </FieldGroup>

      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {t("identity.actions.save")}
        </Button>
      </div>
    </form>
  )
}
