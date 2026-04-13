import type {
  GovernanceSettingModel,
  UpdateGovernanceSettingModel,
} from "@/@types/models"
import type { ApiResponse } from "@/@types/models/common"
import { api } from "@/lib/api"
import { unwrapApiResponse } from "@/lib/api-response"

const baseUrl = "/v1/governance/settings/global"

export const governanceService = {
  getGlobalSetting: () =>
    api
      .get<ApiResponse<GovernanceSettingModel>>(baseUrl)
      .then((res) => unwrapApiResponse(res.data)),
  updateGlobalSetting: (payload: UpdateGovernanceSettingModel) =>
    api
      .put<ApiResponse<GovernanceSettingModel>>(baseUrl, payload)
      .then((res) => unwrapApiResponse(res.data)),
}
