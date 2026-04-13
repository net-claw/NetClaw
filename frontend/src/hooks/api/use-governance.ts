import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"

import type { UpdateGovernanceSettingModel } from "@/@types/models"
import { governanceService } from "@/services/api/governance-service"

export const GOVERNANCE_QUERY_KEYS = {
  global: ["UseGetGlobalGovernanceSetting"],
}

export const useGetGlobalGovernanceSetting = () =>
  useQuery({
    queryKey: GOVERNANCE_QUERY_KEYS.global,
    queryFn: () => governanceService.getGlobalSetting(),
  })

export const useUpdateGlobalGovernanceSetting = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (payload: UpdateGovernanceSettingModel) =>
      governanceService.updateGlobalSetting(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: GOVERNANCE_QUERY_KEYS.global,
      })
    },
  })
}
