import { useNavigate } from "@tanstack/react-router"

import {
  createProviderSchema,
  type CreateProviderModel,
} from "@/@types/models"
import { PageHeaderCard } from "@/components/share/cards/page-header-card"
import { SectionCard } from "@/components/share/cards/section-card"
import { Button } from "@/components/ui/button"
import { useCreateProvider } from "@/hooks/api/use-provider"
import { actionIcons, appIcons } from "@/lib/icons"
import { ProviderForm } from "@/pages/auth/providers/provider-form"
import { useTranslation } from "react-i18next"

export default function ProvidersCreatePage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const createProviderMutation = useCreateProvider()
  const ProvidersIcon = appIcons.providers
  const BackIcon = actionIcons.back

  const handleSubmit = async (values: CreateProviderModel) => {
    const provider = await createProviderMutation.mutateAsync(values)
    navigate({
      to: "/providers/$providerId/edit",
      params: { providerId: provider.id },
    })
  }

  return (
    <>
      <PageHeaderCard
        icon={<ProvidersIcon />}
        title={t("providers.sheet.createTitle")}
        description={t("providers.sheet.createDescription")}
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
        <ProviderForm
          schema={createProviderSchema}
          pending={createProviderMutation.isPending}
          onSubmit={handleSubmit}
        />
      </SectionCard>
    </>
  )
}
