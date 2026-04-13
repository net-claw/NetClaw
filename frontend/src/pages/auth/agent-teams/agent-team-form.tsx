import { zodResolver } from "@hookform/resolvers/zod"
import { useEffect } from "react"
import {
  Controller,
  type Resolver,
  useFieldArray,
  useForm,
} from "react-hook-form"

import type { AgentModel, CreateAgentTeamModel } from "@/@types/models"
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
import { actionIcons } from "@/lib/icons"
import { useTranslation } from "react-i18next"
import type { z } from "zod"

type AgentTeamFormValues = CreateAgentTeamModel

type AgentTeamFormProps = {
  initialValues?: Partial<AgentTeamFormValues>
  pending?: boolean
  agents: AgentModel[]
  schema: z.ZodTypeAny
  onSubmit: (values: AgentTeamFormValues) => void
}

function createMemberId(index: number) {
  return `member-${index + 1}`
}

const emptyValues: AgentTeamFormValues = {
  name: "",
  description: "",
  status: "active",
  metadataJson: "",
  members: [
    {
      id: createMemberId(0),
      agentId: "",
      role: "writer",
      order: 0,
      status: "active",
      reportsToMemberId: "",
      metadataJson: "",
    },
  ],
}

export function AgentTeamForm({
  initialValues,
  pending = false,
  agents,
  schema,
  onSubmit,
}: AgentTeamFormProps) {
  const { t } = useTranslation()
  const AddIcon = actionIcons.create
  const DeleteIcon = actionIcons.delete
  const form = useForm<AgentTeamFormValues>({
    resolver: zodResolver(
      schema as never
    ) as unknown as Resolver<AgentTeamFormValues>,
    defaultValues: {
      ...emptyValues,
      ...initialValues,
      members:
        initialValues?.members && initialValues.members.length > 0
          ? initialValues.members
          : emptyValues.members,
    },
  })

  const {
    fields: memberFields,
    append: appendMember,
    remove: removeMember,
  } = useFieldArray({
    control: form.control,
    name: "members",
  })

  useEffect(() => {
    form.reset({
      ...emptyValues,
      ...initialValues,
      members:
        initialValues?.members && initialValues.members.length > 0
          ? initialValues.members
          : emptyValues.members,
    })
  }, [form, initialValues])

  const memberOptions = form.watch("members").map((member, index) => {
    const agent = agents.find((item) => item.id === member.agentId)

    return {
      id: member.id || createMemberId(index),
      label: member.role || agent?.name || `Member ${index + 1}`,
    }
  })

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
            label={t("agentTeams.form.name")}
            placeholder={t("agentTeams.form.namePlaceholder")}
          />

          <Controller
            name="status"
            control={form.control}
            render={({ field }) => (
              <Field>
                <FieldLabel>{t("agentTeams.form.status")}</FieldLabel>
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger>
                    <SelectValue
                      placeholder={t("agentTeams.form.statusPlaceholder")}
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

        <ControlledField
          name="description"
          control={form.control}
          label={t("agentTeams.form.description")}
          placeholder={t("agentTeams.form.descriptionPlaceholder")}
          multiline
          rows={4}
        />

        <ControlledField
          name="metadataJson"
          control={form.control}
          label={t("agentTeams.form.metadataJson")}
          placeholder={t("agentTeams.form.metadataJsonPlaceholder")}
          description={t("agentTeams.form.metadataJsonHelp")}
          multiline
          rows={6}
        />
      </FieldGroup>

      <FieldGroup>
        <div className="flex items-center justify-between gap-4">
          <div>
            <FieldLabel>Members</FieldLabel>
            <FieldDescription>
              Add agents, set their role, and define who they report to.
            </FieldDescription>
          </div>

          <Button
            type="button"
            variant="outline"
            onClick={() =>
              appendMember({
                id: createMemberId(memberFields.length),
                agentId: "",
                role: "",
                order: memberFields.length,
                status: "active",
                reportsToMemberId: "",
                metadataJson: "",
              })
            }
          >
            <AddIcon data-icon="inline-start" />
            Add member
          </Button>
        </div>

        <div className="flex flex-col gap-4">
          {memberFields.map((field, index) => {
            const currentMemberId = form.watch(`members.${index}.id`) || field.id

            return (
              <div key={field.id} className="rounded-xl border p-4">
                <div className="mb-4 flex items-center justify-between gap-4">
                  <p className="text-sm font-medium">Member {index + 1}</p>
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    disabled={memberFields.length === 1}
                    onClick={() => removeMember(index)}
                  >
                    <DeleteIcon data-icon="inline-start" />
                    Remove
                  </Button>
                </div>

                <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
                  <Controller
                    name={`members.${index}.agentId`}
                    control={form.control}
                    render={({ field: memberField }) => (
                      <Field>
                        <FieldLabel>Agent</FieldLabel>
                        <Select
                          value={memberField.value || "__none__"}
                          onValueChange={(value) =>
                            memberField.onChange(value === "__none__" ? "" : value)
                          }
                          disabled={agents.length === 0}
                        >
                          <SelectTrigger>
                            <SelectValue placeholder="Select agent" />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="__none__">None</SelectItem>
                            {agents.map((agent) => (
                              <SelectItem key={agent.id} value={agent.id}>
                                {agent.name}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      </Field>
                    )}
                  />

                  <ControlledField
                    name={`members.${index}.role`}
                    control={form.control}
                    label="Role"
                    placeholder="writer / lead / reviewer"
                  />

                  <ControlledField
                    name={`members.${index}.order`}
                    control={form.control}
                    label="Order"
                    placeholder="0"
                    type="number"
                  />

                  <Controller
                    name={`members.${index}.status`}
                    control={form.control}
                    render={({ field: memberField }) => (
                      <Field>
                        <FieldLabel>Status</FieldLabel>
                        <Select
                          value={memberField.value}
                          onValueChange={memberField.onChange}
                        >
                          <SelectTrigger>
                            <SelectValue placeholder="Select status" />
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

                  <Controller
                    name={`members.${index}.reportsToMemberId`}
                    control={form.control}
                    render={({ field: memberField }) => (
                      <Field>
                        <FieldLabel>Reports to</FieldLabel>
                        <Select
                          value={memberField.value || "__root__"}
                          onValueChange={(value) =>
                            memberField.onChange(value === "__root__" ? "" : value)
                          }
                        >
                          <SelectTrigger>
                            <SelectValue placeholder="Select manager" />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="__root__">Root member</SelectItem>
                            {memberOptions
                              .filter((member) => member.id !== currentMemberId)
                              .map((member) => (
                                <SelectItem key={member.id} value={member.id}>
                                  {member.label}
                                </SelectItem>
                              ))}
                          </SelectContent>
                        </Select>
                      </Field>
                    )}
                  />
                </div>
              </div>
            )
          })}
        </div>
      </FieldGroup>

      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {t("identity.actions.save")}
        </Button>
      </div>
    </form>
  )
}
