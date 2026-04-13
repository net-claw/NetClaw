import type {
  ChannelModel,
  CreateChannelModel,
  GetChannelRequestModel,
  UpdateChannelModel,
} from "@/@types/models"
import type { ApiResponse, PagedResponse } from "@/@types/models/common"
import { api } from "@/lib/api"
import { unwrapApiResponse } from "@/lib/api-response"

const baseUrl = "/v1/channels"

export const channelService = {
  getById: (channelId: string) =>
    api
      .get<ApiResponse<ChannelModel>>(`${baseUrl}/${channelId}`)
      .then((res) => unwrapApiResponse(res.data)),
  getList: (params: GetChannelRequestModel) =>
    api
      .get<ApiResponse<PagedResponse<ChannelModel>>>(baseUrl, { params })
      .then((res) => unwrapApiResponse(res.data)),
  createChannel: (payload: CreateChannelModel) =>
    api
      .post<ApiResponse<ChannelModel>>(baseUrl, payload)
      .then((res) => unwrapApiResponse(res.data)),
  updateChannel: (channelId: string, payload: UpdateChannelModel) =>
    api
      .put<ApiResponse<ChannelModel>>(`${baseUrl}/${channelId}`, payload)
      .then((res) => unwrapApiResponse(res.data)),
  deleteChannel: (channelId: string) => api.delete(`${baseUrl}/${channelId}`),
  startChannel: (channelId: string) =>
    api
      .post<ApiResponse<ChannelModel>>(`${baseUrl}/${channelId}/start`)
      .then((res) => unwrapApiResponse(res.data)),
  stopChannel: (channelId: string) =>
    api
      .post<ApiResponse<ChannelModel>>(`${baseUrl}/${channelId}/stop`)
      .then((res) => unwrapApiResponse(res.data)),
  restartChannel: (channelId: string) =>
    api
      .post<ApiResponse<ChannelModel>>(`${baseUrl}/${channelId}/restart`)
      .then((res) => unwrapApiResponse(res.data)),
}
