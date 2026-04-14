import { zodResolver } from "@hookform/resolvers/zod"
import { useEffect } from "react"
import { Controller, type Resolver, useForm, useWatch } from "react-hook-form"
import { useTranslation } from "react-i18next"
import type { z } from "zod"

import type { CreateProviderModel } from "@/@types/models"
import { ControlledField } from "@/components/form/controlled-field"
import { Button } from "@/components/ui/button"
import { Checkbox } from "@/components/ui/checkbox"
import {
  Field,
  FieldDescription,
  FieldGroup,
  FieldLabel,
} from "@/components/ui/field"
import { Input } from "@/components/ui/input"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { providerData } from "@/constants/data"

type ProviderFormValues = CreateProviderModel

// _modelSelect is internal UI state stored in form to avoid useState-in-effect issues
type ProviderFormInternalValues = ProviderFormValues & { _modelSelect: string }

type ProviderFormProps = {
  initialValues?: Partial<ProviderFormValues>
  pending?: boolean
  schema: z.ZodTypeAny
  isEdit?: boolean
  onSubmit: (values: ProviderFormValues) => void
}

const emptyValues: ProviderFormInternalValues = {
  name: "",
  provider: "openai",
  model: "",
  apiKey: "",
  baseUrl: "",
  active: true,
  _modelSelect: "",
}

function resolveModelSelectValue(model: string, knownModels: string[]) {
  if (!model) return ""
  return knownModels.includes(model) ? model : "other"
}

export function ProviderForm({
  initialValues,
  pending = false,
  schema,
  isEdit = false,
  onSubmit,
}: ProviderFormProps) {
  const { t } = useTranslation()

  const form = useForm<ProviderFormInternalValues>({
    resolver: zodResolver(
      schema as never
    ) as unknown as Resolver<ProviderFormInternalValues>,
    defaultValues: {
      ...emptyValues,
      ...initialValues,
      _modelSelect: resolveModelSelectValue(
        initialValues?.model ?? "",
        providerData.find((p) => p.value === initialValues?.provider)?.models ??
          []
      ),
    },
  })

  const selectedProvider = useWatch({ control: form.control, name: "provider" })
  const currentModel = useWatch({ control: form.control, name: "model" })
  const modelSelectValue = useWatch({
    control: form.control,
    name: "_modelSelect",
  })

  const providerModels =
    providerData.find((p) => p.value === selectedProvider)?.models ?? []

  useEffect(() => {
    const models =
      providerData.find((p) => p.value === initialValues?.provider)?.models ??
      []
    form.reset({
      ...emptyValues,
      ...initialValues,
      _modelSelect: resolveModelSelectValue(initialValues?.model ?? "", models),
    })
  }, [form, initialValues])

  function handleProviderChange(
    value: string,
    fieldOnChange: (v: string) => void
  ) {
    fieldOnChange(value)
    form.setValue("model", "")
    form.setValue("_modelSelect", "")
  }

  function handleModelSelectChange(value: string) {
    form.setValue("_modelSelect", value)
    if (value !== "other") {
      form.setValue("model", value, { shouldValidate: true })
    } else {
      form.setValue("model", "", { shouldValidate: false })
    }
  }

  const isOtherModel = modelSelectValue === "other"

  return (
    <form
      className="flex flex-col gap-6"
      onSubmit={form.handleSubmit((values) => {
        const { _modelSelect, ...rest } = values
        void _modelSelect
        onSubmit(rest)
      })}
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
                <Select
                  value={field.value}
                  onValueChange={(v) => handleProviderChange(v, field.onChange)}
                >
                  <SelectTrigger>
                    <SelectValue
                      placeholder={t("providers.form.selectProvider")}
                    />
                  </SelectTrigger>
                  <SelectContent>
                    {providerData.map((p) => (
                      <SelectItem key={p.value} value={p.value}>
                        <div className="flex items-center gap-2">
                          {p.image && (
                            <img
                              src={p.image}
                              alt={p.label}
                              className="h-8 w-8 object-contain"
                            />
                          )}
                          {p.label}
                        </div>
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </Field>
            )}
          />
        </div>

        <div className="grid gap-5 md:grid-cols-2">
          <Field>
            <FieldLabel>{t("providers.form.model")}</FieldLabel>
            <Select
              value={modelSelectValue}
              onValueChange={handleModelSelectChange}
            >
              <SelectTrigger>
                <SelectValue
                  placeholder={t("providers.form.modelPlaceholder")}
                />
              </SelectTrigger>
              <SelectContent>
                {providerModels.map((m) => (
                  <SelectItem key={m} value={m}>
                    {m}
                  </SelectItem>
                ))}
                <SelectItem value="other">
                  {t("providers.form.modelOther", { defaultValue: "Other..." })}
                </SelectItem>
              </SelectContent>
            </Select>
            {isOtherModel && (
              <Input
                className="mt-2"
                value={currentModel}
                placeholder={t("providers.form.modelCustomPlaceholder", {
                  defaultValue: "Enter model name...",
                })}
                onChange={(e) =>
                  form.setValue("model", e.target.value, {
                    shouldValidate: true,
                  })
                }
              />
            )}
            {form.formState.errors.model && (
              <p className="text-sm text-destructive">
                {form.formState.errors.model.message}
              </p>
            )}
          </Field>

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
          placeholder={t("providers.form.apiKeyPlaceholder")}
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
