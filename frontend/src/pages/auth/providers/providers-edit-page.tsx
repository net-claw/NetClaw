import { useNavigate } from "@tanstack/react-router"

import {
  updateProviderSchema,
  type UpdateProviderModel,
} from "@/@types/models"
import { PageHeaderCard } from "@/components/share/cards/page-header-card"
import { SectionCard } from "@/components/share/cards/section-card"
import { Button } from "@/components/ui/button"
import { useGetProviderById, useUpdateProvider } from "@/hooks/api/use-provider"
import { actionIcons, appIcons } from "@/lib/icons"
import { ProviderForm } from "@/pages/auth/providers/provider-form"
import { useTranslation } from "react-i18next"

type ProvidersEditPageProps = {
  providerId: string
}

export default function ProvidersEditPage({
  providerId,
}: ProvidersEditPageProps) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const ProvidersIcon = appIcons.providers
  const BackIcon = actionIcons.back
  const { data: provider, isLoading } = useGetProviderById(providerId)
  const updateProviderMutation = useUpdateProvider()

  const handleSubmit = async (values: UpdateProviderModel) => {
    await updateProviderMutation.mutateAsync({
      providerId,
      payload: values,
    })
  }

  return (
    <>
      <PageHeaderCard
        icon={<ProvidersIcon />}
        title={t("providers.sheet.editTitle")}
        description={t("providers.sheet.editDescription")}
        headerRight={
          <Button
            type="button"
            variant="outline"
            onClick={() => navigate({ to: "/providers" })}
          >
            <BackIcon data-icon="inline-start" />
            {t("providers.sheet.cancel")}
          </Button>
        }
      />

      <SectionCard contentClassName="flex flex-col gap-6">
        {isLoading || !provider ? (
          <p className="text-sm text-muted-foreground">
            {t("providers.errors.loadProviders")}
          </p>
        ) : (
          <ProviderForm
            schema={updateProviderSchema}
            pending={updateProviderMutation.isPending}
            isEdit
            initialValues={{
              name: provider.name,
              provider: provider.providerType as UpdateProviderModel["provider"],
              model: provider.defaultModel,
              apiKey: "",
              baseUrl: provider.baseUrl ?? "",
              active: provider.isActive,
            }}
            onSubmit={handleSubmit}
          />
        )}
      </SectionCard>
    </>
  )
}
