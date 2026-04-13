import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"

import type {
  CreateProviderModel,
  GetProviderRequestModel,
  UpdateProviderModel,
} from "@/@types/models"
import { providerService } from "@/services/api/provider-service"

export const PROVIDER_QUERY_KEYS = {
  UseGetProviderList: (params?: GetProviderRequestModel) => [
    "UseGetProviderList",
    ...Object.values(params || {}),
  ],
  UseGetProviderById: (providerId: string) => ["UseGetProviderById", providerId],
}

export const useGetProviderList = (params: GetProviderRequestModel) =>
  useQuery({
    queryKey: PROVIDER_QUERY_KEYS.UseGetProviderList(params),
    queryFn: () => providerService.getList(params),
  })

export const useGetProviderById = (providerId: string) =>
  useQuery({
    queryKey: PROVIDER_QUERY_KEYS.UseGetProviderById(providerId),
    queryFn: () => providerService.getById(providerId),
    enabled: Boolean(providerId),
  })

export const useCreateProvider = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (payload: CreateProviderModel) =>
      providerService.createProvider(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["UseGetProviderList"] })
    },
  })
}

export const useUpdateProvider = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({
      providerId,
      payload,
    }: {
      providerId: string
      payload: UpdateProviderModel
    }) => providerService.updateProvider(providerId, payload),
    onSuccess: (_result, variables) => {
      void queryClient.invalidateQueries({ queryKey: ["UseGetProviderList"] })
      void queryClient.invalidateQueries({
        queryKey: PROVIDER_QUERY_KEYS.UseGetProviderById(variables.providerId),
      })
    },
  })
}

export const useDeleteProviders = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (providerIds: string[]) => {
      const results = await Promise.allSettled(
        providerIds.map((providerId) => providerService.deleteProvider(providerId))
      )

      const failedCount = results.filter(
        (result) => result.status === "rejected"
      ).length

      if (failedCount > 0) {
        throw new Error(
          failedCount === providerIds.length
            ? "Delete providers failed."
            : `Deleted ${providerIds.length - failedCount}/${providerIds.length} providers.`
        )
      }

      return results
    },
    meta: { skipErrorToast: true },
    onSuccess: (_result, providerIds) => {
      void queryClient.invalidateQueries({ queryKey: ["UseGetProviderList"] })
      toast.success(
        providerIds.length === 1
          ? "Provider deleted."
          : `${providerIds.length} providers deleted.`
      )
    },
    onError: (error) => {
      toast.error(
        error instanceof Error ? error.message : "Delete providers failed."
      )
    },
  })
}
