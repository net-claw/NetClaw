import type {
  CreateProviderModel,
  GetProviderRequestModel,
  ProviderModel,
  UpdateProviderModel,
} from "@/@types/models"
import type { ApiResponse, PagedResponse } from "@/@types/models/common"
import { api } from "@/lib/api"
import { unwrapApiResponse } from "@/lib/api-response"

const baseUrl = "/v1/providers"

export const providerService = {
  getList: (params: GetProviderRequestModel) =>
    api
      .get<ApiResponse<PagedResponse<ProviderModel>>>(baseUrl, { params })
      .then((res) => unwrapApiResponse(res.data)),
  getById: (providerId: string) =>
    api
      .get<ApiResponse<ProviderModel>>(`${baseUrl}/${providerId}`)
      .then((res) => unwrapApiResponse(res.data)),
  createProvider: (payload: CreateProviderModel) =>
    api
      .post<ApiResponse<ProviderModel>>(baseUrl, payload)
      .then((res) => unwrapApiResponse(res.data)),
  updateProvider: (providerId: string, payload: UpdateProviderModel) =>
    api
      .put<ApiResponse<ProviderModel>>(`${baseUrl}/${providerId}`, payload)
      .then((res) => unwrapApiResponse(res.data)),
  deleteProvider: (providerId: string) => api.delete(`${baseUrl}/${providerId}`),
}
