import type {
  ConversationListItemModel,
  ConversationListRequestModel,
  ConversationModel,
} from "@/@types/models"
import type { ApiResponse } from "@/@types/models/common"
import { api } from "@/lib/api"
import { unwrapApiResponse } from "@/lib/api-response"

const baseUrl = "/v1/conversations"

export const conversationService = {
  getList: (params: ConversationListRequestModel) =>
    api
      .get<ApiResponse<ConversationListItemModel[]>>(baseUrl, { params })
      .then((res) => unwrapApiResponse(res.data)),
  getById: (conversationId: string) =>
    api
      .get<ApiResponse<ConversationModel>>(`${baseUrl}/${conversationId}`)
      .then((res) => unwrapApiResponse(res.data)),
}
