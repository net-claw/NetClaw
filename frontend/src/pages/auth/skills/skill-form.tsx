import { zodResolver } from "@hookform/resolvers/zod"
import { useEffect } from "react"
import { Controller, type Resolver, useForm } from "react-hook-form"
import { z } from "zod"

import type { CreateSkillModel } from "@/@types/models"
import { ControlledField } from "@/components/form/controlled-field"
import { Button } from "@/components/ui/button"
import {
  Field,
  FieldDescription,
  FieldGroup,
  FieldLabel,
} from "@/components/ui/field"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { useTranslation } from "react-i18next"

type SkillFormValues = CreateSkillModel

type SkillFormProps = {
  initialValues?: Partial<SkillFormValues>
  pending?: boolean
  schema: z.ZodTypeAny
  onSubmit: (values: SkillFormValues) => void
}

const emptyValues: SkillFormValues = {
  name: "",
  slug: "",
  description: "",
  fileName: "",
  content: "",
  status: "active",
  metadataJson: "",
}

export function SkillForm({
  initialValues,
  pending = false,
  schema,
  onSubmit,
}: SkillFormProps) {
  const { t } = useTranslation()
  const form = useForm<SkillFormValues>({
    resolver: zodResolver(
      schema as never
    ) as unknown as Resolver<SkillFormValues>,
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

  const handleSave = form.handleSubmit((values) => onSubmit(values))

  return (
    <form className="flex flex-col gap-6" onSubmit={handleSave}>
      <FieldGroup>
        <div className="grid gap-5 md:grid-cols-2">
          <ControlledField
            name="name"
            control={form.control}
            label={t("skills.form.name")}
            placeholder={t("skills.form.namePlaceholder")}
          />
          <ControlledField
            name="slug"
            control={form.control}
            label="Slug"
            placeholder="excel-export"
          />
        </div>

        <ControlledField
          name="description"
          control={form.control}
          label={t("skills.form.description")}
          placeholder={t("skills.form.descriptionPlaceholder")}
          multiline
        />

        <div className="grid gap-5 md:grid-cols-2">
          <ControlledField
            name="fileName"
            control={form.control}
            label={t("skills.form.fileName")}
            placeholder={t("skills.form.fileNamePlaceholder")}
            description={t("skills.form.fileNameHelp")}
          />

          <Controller
            name="status"
            control={form.control}
            render={({ field }) => (
              <Field>
                <FieldLabel>{t("skills.form.status")}</FieldLabel>
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="active">
                      {t("skills.status.active")}
                    </SelectItem>
                    <SelectItem value="paused">
                      {t("skills.status.paused")}
                    </SelectItem>
                    <SelectItem value="archived">
                      {t("skills.status.archived")}
                    </SelectItem>
                  </SelectContent>
                </Select>
                <FieldDescription>{t("skills.form.status")}</FieldDescription>
              </Field>
            )}
          />
        </div>

        <ControlledField
          name="content"
          control={form.control}
          label={t("skills.form.content")}
          placeholder={t("skills.form.contentPlaceholder")}
          description={t("skills.form.contentHelp")}
          multiline
          rows={16}
        />

        <ControlledField
          name="metadataJson"
          control={form.control}
          label={t("skills.form.metadataJson")}
          placeholder={t("skills.form.metadataJsonPlaceholder")}
          description={t("skills.form.metadataJsonHelp")}
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
