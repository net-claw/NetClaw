import { zodResolver } from "@hookform/resolvers/zod"
import { useEffect, useState } from "react"
import { Controller, type Resolver, useForm } from "react-hook-form"
import { useTranslation } from "react-i18next"
import { z } from "zod"
import type { z as zType } from "zod"

import type { CreateChannelModel } from "@/@types/models"
import { ControlledField } from "@/components/form/controlled-field"
import { Button } from "@/components/ui/button"
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

type ChannelFormInternalValues = CreateChannelModel & { appToken?: string }

type ChannelFormProps = {
  initialValues?: Partial<ChannelFormInternalValues>
  pending?: boolean
  schema: zType.ZodTypeAny
  isEdit?: boolean
  onSubmit: (values: CreateChannelModel, startNow: boolean) => void
}

const emptyValues: ChannelFormInternalValues = {
  name: "",
  kind: "telegram",
  token: "",
  appToken: "",
  settingsJson: "",
  startNow: false,
}

export function ChannelForm({
  initialValues,
  pending = false,
  schema,
  isEdit = false,
  onSubmit,
}: ChannelFormProps) {
  const { t } = useTranslation()
  const [submitMode, setSubmitMode] = useState<"create" | "create-and-start">(
    "create"
  )

  const extendedSchema = schema instanceof z.ZodObject
    ? schema.extend({ appToken: z.string().optional() })
    : schema

  const form = useForm<ChannelFormInternalValues>({
    resolver: zodResolver(
      extendedSchema as never
    ) as unknown as Resolver<ChannelFormInternalValues>,
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

  const kind = form.watch("kind")

  const handleSubmit = form.handleSubmit((values) => {
    const { appToken, ...rest } = values
    let token = rest.token ?? ""
    if (rest.kind === "slack" && appToken) {
      token = JSON.stringify({ BotToken: token, AppToken: appToken })
    }
    onSubmit(
      { ...rest, token },
      !isEdit && submitMode === "create-and-start"
    )
  })

  return (
    <form className="flex flex-col gap-6" onSubmit={handleSubmit}>
      <FieldGroup>
        <div className="grid gap-5 md:grid-cols-2">
          <ControlledField
            name="name"
            control={form.control}
            label={t("channels.form.name")}
            placeholder={t("channels.form.namePlaceholder")}
          />

          <Controller
            name="kind"
            control={form.control}
            render={({ field }) => (
              <Field>
                <FieldLabel>{t("channels.form.kind")}</FieldLabel>
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger>
                    <SelectValue
                      placeholder={t("channels.form.kindPlaceholder")}
                    />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="telegram">
                      {t("channels.options.kind.telegram")}
                    </SelectItem>
                    <SelectItem value="discord">
                      {t("channels.options.kind.discord")}
                    </SelectItem>
                    <SelectItem value="whatsapp">
                      {t("channels.options.kind.whatsapp")}
                    </SelectItem>
                    <SelectItem value="slack">
                      {t("channels.options.kind.slack")}
                    </SelectItem>
                    <SelectItem value="web">
                      {t("channels.options.kind.web")}
                    </SelectItem>
                  </SelectContent>
                </Select>
              </Field>
            )}
          />
        </div>

        <ControlledField
          name="token"
          control={form.control}
          label={t("channels.form.token")}
          placeholder={t("channels.form.tokenPlaceholder")}
          description={
            isEdit
              ? t("channels.form.tokenEditHelp")
              : t("channels.form.tokenHelp")
          }
        />

        {kind === "slack" && (
          <ControlledField
            name="appToken"
            control={form.control}
            label={t("channels.form.appToken")}
            placeholder={t("channels.form.appTokenPlaceholder")}
            description={
              isEdit
                ? t("channels.form.appTokenEditHelp")
                : t("channels.form.appTokenHelp")
            }
          />
        )}

        <ControlledField
          name="settingsJson"
          control={form.control}
          label={t("channels.form.settingsJson")}
          placeholder={t("channels.form.settingsJsonPlaceholder")}
          description={<FieldDescription>{t("channels.form.settingsJsonHelp")}</FieldDescription>}
          multiline
          rows={6}
        />
      </FieldGroup>

      <div className="flex justify-end gap-3">
        <Button
          type="submit"
          disabled={pending}
          onClick={() => setSubmitMode("create")}
        >
          {isEdit ? t("identity.actions.save") : t("channels.actions.create")}
        </Button>
        {!isEdit ? (
          <Button
            type="submit"
            variant="outline"
            disabled={pending}
            onClick={() => setSubmitMode("create-and-start")}
          >
            {t("channels.actions.createAndStart")}
          </Button>
        ) : null}
      </div>
    </form>
  )
}
