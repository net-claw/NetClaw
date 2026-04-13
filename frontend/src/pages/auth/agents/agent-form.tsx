import { zodResolver } from "@hookform/resolvers/zod"
import { useEffect } from "react"
import { Controller, type Resolver, useForm } from "react-hook-form"

import type {
  CreateAgentModel,
  ProviderModel,
  SkillModel,
} from "@/@types/models"
import { ControlledField } from "@/components/form/controlled-field"
import { Button } from "@/components/ui/button"
import { Checkbox } from "@/components/ui/checkbox"
import {
  Field,
  FieldDescription,
  FieldGroup,
  FieldLabel,
} from "@/components/ui/field"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { actionIcons } from "@/lib/icons"
import { useTranslation } from "react-i18next"
import type { z } from "zod"

type AgentFormValues = CreateAgentModel

type AgentFormProps = {
  initialValues?: Partial<AgentFormValues>
  pending?: boolean
  providers: ProviderModel[]
  skills: SkillModel[]
  schema: z.ZodTypeAny
  onSubmit: (values: AgentFormValues) => void
}

const emptyValues: AgentFormValues = {
  name: "",
  role: "",
  kind: "worker",
  type: "",
  status: "active",
  providerIds: [],
  skillIds: [],
  modelOverride: "",
  systemPrompt: "",
  temperature: 0.7,
  maxTokens: 1024,
  metadataJson: "",
}

export function AgentForm({
  initialValues,
  pending = false,
  providers,
  skills,
  schema,
  onSubmit,
}: AgentFormProps) {
  const { t } = useTranslation()
  const UpIcon = actionIcons.back
  const DownIcon = actionIcons.refresh
  const form = useForm<AgentFormValues>({
    resolver: zodResolver(
      schema as never
    ) as unknown as Resolver<AgentFormValues>,
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

  const selectedProviderIds = form.watch("providerIds")
  const selectedSkillIds = form.watch("skillIds")

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
            label={t("agents.form.name")}
            placeholder={t("agents.form.namePlaceholder")}
          />
          <ControlledField
            name="role"
            control={form.control}
            label={t("agents.form.role")}
            placeholder={t("agents.form.rolePlaceholder")}
          />
        </div>

        <div className="grid gap-5 md:grid-cols-3">
          <Controller
            name="kind"
            control={form.control}
            render={({ field }) => (
              <Field>
                <FieldLabel>{t("agents.form.kind")}</FieldLabel>
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger>
                    <SelectValue placeholder={t("agents.form.kindPlaceholder")} />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="worker">
                      {t("agents.form.kindOptions.worker")}
                    </SelectItem>
                    <SelectItem value="orchestrator">
                      {t("agents.form.kindOptions.orchestrator")}
                    </SelectItem>
                  </SelectContent>
                </Select>
                <FieldDescription>{t("agents.form.kindHelp")}</FieldDescription>
              </Field>
            )}
          />

          <ControlledField
            name="type"
            control={form.control}
            label={t("agents.form.type")}
            placeholder={t("agents.form.typePlaceholder")}
          />

          <Controller
            name="status"
            control={form.control}
            render={({ field }) => (
              <Field>
                <FieldLabel>{t("agents.form.status")}</FieldLabel>
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger>
                    <SelectValue
                      placeholder={t("agents.form.statusPlaceholder")}
                    />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="active">active</SelectItem>
                    <SelectItem value="paused">paused</SelectItem>
                    <SelectItem value="archived">archived</SelectItem>
                  </SelectContent>
                </Select>
              </Field>
            )}
          />
        </div>

        <Field>
          <FieldLabel>{t("agents.form.providers")}</FieldLabel>
          <FieldDescription>{t("agents.form.providersHelp")}</FieldDescription>
          <div className="mt-3 flex flex-col gap-3">
            {providers.length === 0 ? (
              <p className="text-sm text-muted-foreground">
                {t("agents.form.noProvidersConfigured")}
              </p>
            ) : (
              providers.map((provider) => {
                const index = selectedProviderIds.indexOf(provider.id)
                const isSelected = index >= 0

                return (
                  <div
                    key={provider.id}
                    className="flex items-center justify-between rounded-lg border p-3"
                  >
                    <div className="flex items-start gap-3">
                      <Checkbox
                        checked={isSelected}
                        onCheckedChange={(checked) => {
                          const next = [...selectedProviderIds]

                          if (checked) {
                            if (!next.includes(provider.id)) {
                              next.push(provider.id)
                            }
                          } else {
                            const providerIndex = next.indexOf(provider.id)
                            if (providerIndex >= 0) {
                              next.splice(providerIndex, 1)
                            }
                          }

                          form.setValue("providerIds", next, {
                            shouldDirty: true,
                            shouldValidate: true,
                          })
                        }}
                      />
                      <div>
                        <p className="font-medium">{provider.name}</p>
                        <p className="text-sm text-muted-foreground">
                          {provider.providerType} • {provider.defaultModel}
                        </p>
                        {isSelected ? (
                          <p className="text-xs text-muted-foreground">
                            {t("agents.form.providerPriority", {
                              priority: index + 1,
                            })}
                          </p>
                        ) : null}
                      </div>
                    </div>

                    {isSelected ? (
                      <div className="flex gap-2">
                        <Button
                          type="button"
                          size="sm"
                          variant="outline"
                          disabled={index === 0}
                          onClick={() => {
                            const next = [...selectedProviderIds]
                            ;[next[index - 1], next[index]] = [
                              next[index],
                              next[index - 1],
                            ]
                            form.setValue("providerIds", next, {
                              shouldDirty: true,
                              shouldValidate: true,
                            })
                          }}
                        >
                          <UpIcon className="size-4 rotate-90" />
                        </Button>
                        <Button
                          type="button"
                          size="sm"
                          variant="outline"
                          disabled={index === selectedProviderIds.length - 1}
                          onClick={() => {
                            const next = [...selectedProviderIds]
                            ;[next[index], next[index + 1]] = [
                              next[index + 1],
                              next[index],
                            ]
                            form.setValue("providerIds", next, {
                              shouldDirty: true,
                              shouldValidate: true,
                            })
                          }}
                        >
                          <DownIcon className="size-4 rotate-90" />
                        </Button>
                      </div>
                    ) : null}
                  </div>
                )
              })
            )}
          </div>
        </Field>

        <Field>
          <FieldLabel>{t("agents.skills.title")}</FieldLabel>
          <FieldDescription>{t("agents.skills.description")}</FieldDescription>
          <div className="mt-3 flex flex-col gap-3">
            {skills.length === 0 ? (
              <p className="text-sm text-muted-foreground">
                {t("agents.form.noSkillsConfigured")}
              </p>
            ) : (
              skills.map((skill) => {
                const isSelected = selectedSkillIds.includes(skill.id)

                return (
                  <div
                    key={skill.id}
                    className="flex items-center justify-between rounded-lg border p-3"
                  >
                    <div className="flex items-start gap-3">
                      <Checkbox
                        checked={isSelected}
                        onCheckedChange={(checked) => {
                          const next = checked
                            ? [...selectedSkillIds, skill.id]
                            : selectedSkillIds.filter((id) => id !== skill.id)

                          form.setValue("skillIds", Array.from(new Set(next)), {
                            shouldDirty: true,
                            shouldValidate: true,
                          })
                        }}
                      />
                      <div>
                        <p className="font-medium">{skill.name}</p>
                        <p className="text-sm text-muted-foreground">
                          {skill.fileName}
                        </p>
                        <p className="text-xs text-muted-foreground">
                          {skill.description}
                        </p>
                      </div>
                    </div>
                  </div>
                )
              })
            )}
          </div>
        </Field>

        <div className="grid gap-5 md:grid-cols-2">
          <ControlledField
            name="modelOverride"
            control={form.control}
            label={t("agents.form.modelOverride")}
            placeholder={t("agents.form.modelOverridePlaceholder")}
            description={t("agents.form.modelOverrideHelp")}
          />
          <ControlledField
            name="temperature"
            control={form.control}
            label={t("agents.form.temperature")}
            placeholder={t("agents.form.temperaturePlaceholder")}
            type="number"
          />
        </div>

        <ControlledField
          name="maxTokens"
          control={form.control}
          label={t("agents.form.maxTokens")}
          placeholder={t("agents.form.maxTokensPlaceholder")}
          type="number"
        />

        <ControlledField
          name="systemPrompt"
          control={form.control}
          label={t("agents.form.systemPrompt")}
          placeholder={t("agents.form.systemPromptPlaceholder")}
          multiline
          rows={10}
        />

        <ControlledField
          name="metadataJson"
          control={form.control}
          label={t("agents.form.metadataJson")}
          placeholder={t("agents.form.metadataJsonPlaceholder")}
          description={t("agents.form.metadataJsonHelp")}
          multiline
          rows={6}
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
