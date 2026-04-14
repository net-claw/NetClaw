import { zodResolver } from "@hookform/resolvers/zod"
import { useEffect, useState } from "react"
import { Controller, type Resolver, useForm } from "react-hook-form"
import { useTranslation } from "react-i18next"
import type { z as zType } from "zod"
import { z } from "zod"

import type { CreateChannelModel } from "@/@types/models"
import { ControlledField } from "@/components/form/controlled-field"
import { useGetAgentById, useGetAgentList } from "@/hooks/api/use-agent"
import {
  useGetAgentTeamById,
  useGetAgentTeamList,
} from "@/hooks/api/use-agent-team"
import { Button } from "@/components/ui/button"
import {
  Field,
  FieldDescription,
  FieldError,
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
import { channelData } from "@/constants/data"

type ChannelReplyTargetType = "agent" | "team"

type ChannelFormInternalValues = CreateChannelModel & {
  appToken?: string
  replyTargetType: ChannelReplyTargetType
  replyTargetId: string
}

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
  agentId: "",
  agentTeamId: "",
  startNow: false,
  replyTargetType: "agent",
  replyTargetId: "",
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
  const { data: agentsData } = useGetAgentList({
    pageIndex: 0,
    pageSize: 100,
    ascending: true,
    orderBy: "name",
    status: "active",
  })
  const { data: teamsData } = useGetAgentTeamList({
    pageIndex: 0,
    pageSize: 100,
    ascending: true,
    orderBy: "name",
    status: "active",
  })
  const currentAgentId = initialValues?.agentId ?? ""
  const currentAgentTeamId = initialValues?.agentTeamId ?? ""
  const { data: selectedAgent } = useGetAgentById(currentAgentId)
  const { data: selectedTeam } = useGetAgentTeamById(currentAgentTeamId)

  const agents = [
    ...(agentsData?.items ?? []),
    ...(selectedAgent &&
    !(agentsData?.items ?? []).some((item) => item.id === selectedAgent.id)
      ? [selectedAgent]
      : []),
  ]
  const teams = [
    ...(teamsData?.items ?? []),
    ...(selectedTeam &&
    !(teamsData?.items ?? []).some((item) => item.id === selectedTeam.id)
      ? [selectedTeam]
      : []),
  ]

  const extendedSchema =
    schema instanceof z.ZodObject
      ? schema.extend({
          appToken: z.string().optional(),
          replyTargetType: z.enum(["agent", "team"]),
          replyTargetId: z.string().min(1, "validation.required"),
        })
      : schema

  const form = useForm<ChannelFormInternalValues>({
    resolver: zodResolver(
      extendedSchema as never
    ) as unknown as Resolver<ChannelFormInternalValues>,
    defaultValues: {
      ...emptyValues,
      replyTargetType: initialValues?.agentTeamId ? "team" : "agent",
      replyTargetId: initialValues?.agentTeamId ?? initialValues?.agentId ?? "",
      ...initialValues,
    },
  })

  useEffect(() => {
    form.reset({
      ...emptyValues,
      replyTargetType: initialValues?.agentTeamId ? "team" : "agent",
      replyTargetId: initialValues?.agentTeamId ?? initialValues?.agentId ?? "",
      ...initialValues,
    })
  }, [form, initialValues])

  const kind = form.watch("kind")
  const replyTargetType = form.watch("replyTargetType")

  const handleSubmit = form.handleSubmit((values) => {
    const { appToken, replyTargetId, replyTargetType, ...rest } = values
    let token = rest.token ?? ""
    if (rest.kind === "slack" && appToken) {
      token = JSON.stringify({ BotToken: token, AppToken: appToken })
    }

    onSubmit(
      {
        ...rest,
        token,
        agentId: replyTargetType === "agent" ? replyTargetId : undefined,
        agentTeamId: replyTargetType === "team" ? replyTargetId : undefined,
      },
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
                    {channelData.map((channel) => (
                      <SelectItem key={channel.value} value={channel.value}>
                        <div className="flex items-center gap-2">
                          <img
                            src={channel.image}
                            alt={channel.label}
                            className="h-6 w-6"
                          />
                          <span>{channel.label}</span>
                        </div>
                      </SelectItem>
                    ))}
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
          description={
            <FieldDescription>
              {t("channels.form.settingsJsonHelp")}
            </FieldDescription>
          }
          multiline
          rows={6}
        />

        <div className="grid gap-5 md:grid-cols-2">
          <Controller
            name="replyTargetType"
            control={form.control}
            render={({ field }) => (
              <Field>
                <FieldLabel>Reply Target Type</FieldLabel>
                <Select
                  value={field.value}
                  onValueChange={(value) => {
                    field.onChange(value as ChannelReplyTargetType)
                    form.setValue("replyTargetId", "", { shouldValidate: true })
                  }}
                >
                  <SelectTrigger>
                    <SelectValue placeholder="Select target type" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="agent">Agent</SelectItem>
                    <SelectItem value="team">Agent Team</SelectItem>
                  </SelectContent>
                </Select>
              </Field>
            )}
          />

          <Controller
            name="replyTargetId"
            control={form.control}
            render={({ field, fieldState }) => (
              <Field data-invalid={fieldState.invalid}>
                <FieldLabel>
                  {replyTargetType === "team" ? "Agent Team" : "Agent"}
                </FieldLabel>
                <Select
                  value={field.value || "__none__"}
                  onValueChange={(value) =>
                    field.onChange(value === "__none__" ? "" : value)
                  }
                  disabled={
                    replyTargetType === "team"
                      ? teams.length === 0
                      : agents.length === 0
                  }
                >
                  <SelectTrigger>
                    <SelectValue
                      placeholder={
                        replyTargetType === "team"
                          ? "Select agent team"
                          : "Select agent"
                      }
                    />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="__none__">None</SelectItem>
                    {(replyTargetType === "team" ? teams : agents).map(
                      (item) => (
                        <SelectItem key={item.id} value={item.id}>
                          {item.name}
                        </SelectItem>
                      )
                    )}
                  </SelectContent>
                </Select>
                <FieldDescription>
                  {replyTargetType === "team"
                    ? "Inbound messages from this channel will be answered by the selected agent team."
                    : "Inbound messages from this channel will be answered by the selected agent."}
                </FieldDescription>
                {fieldState.invalid ? (
                  <FieldError
                    errors={[
                      fieldState.error?.message
                        ? {
                            ...fieldState.error,
                            message: t(fieldState.error.message),
                          }
                        : fieldState.error,
                    ]}
                  />
                ) : null}
              </Field>
            )}
          />
        </div>
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
