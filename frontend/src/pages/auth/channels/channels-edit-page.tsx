import { useNavigate } from "@tanstack/react-router"
import { useTranslation } from "react-i18next"

import {
  updateChannelSchema,
  type UpdateChannelModel,
} from "@/@types/models"
import { PageHeaderCard } from "@/components/share/cards/page-header-card"
import { SectionCard } from "@/components/share/cards/section-card"
import { Button } from "@/components/ui/button"
import { useGetChannelById, useUpdateChannel } from "@/hooks/api/use-channel"
import { actionIcons, appIcons } from "@/lib/icons"
import { ChannelForm } from "@/pages/auth/channels/channel-form"

type ChannelsEditPageProps = {
  channelId: string
}

export default function ChannelsEditPage({
  channelId,
}: ChannelsEditPageProps) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const ChannelsIcon = appIcons.channels
  const BackIcon = actionIcons.back
  const { data: channel, isLoading } = useGetChannelById(channelId)
  const updateChannelMutation = useUpdateChannel()

  const handleSubmit = async (
    values: UpdateChannelModel,
    _startNow: boolean
  ) => {
    await updateChannelMutation.mutateAsync({
      channelId,
      payload: values,
    })
  }

  return (
    <>
      <PageHeaderCard
        icon={<ChannelsIcon />}
        title={t("channels.editTitle")}
        description={t("channels.editDescription")}
        headerRight={
          <Button
            type="button"
            variant="outline"
            onClick={() => navigate({ to: "/channels" })}
          >
            <BackIcon data-icon="inline-start" />
            {t("channels.actions.backToChannels")}
          </Button>
        }
      />

      <SectionCard contentClassName="flex flex-col gap-6">
        {isLoading || !channel ? (
          <p className="text-sm text-muted-foreground">
            {t("channels.errors.loadChannel")}
          </p>
        ) : (
          <ChannelForm
            schema={updateChannelSchema}
            pending={updateChannelMutation.isPending}
            isEdit
            initialValues={{
              name: channel.name,
              kind: channel.kind,
              token: "",
              settingsJson: channel.settingsJson ?? "",
              startNow: false,
            }}
            onSubmit={handleSubmit}
          />
        )}
      </SectionCard>
    </>
  )
}
