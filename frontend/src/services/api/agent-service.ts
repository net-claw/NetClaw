import type {
  AgentModel,
  CreateAgentModel,
  GetAgentRequestModel,
  UpdateAgentModel,
} from "@/@types/models"
import type { ApiResponse, PagedResponse } from "@/@types/models/common"
import { api } from "@/lib/api"
import { unwrapApiResponse } from "@/lib/api-response"

const baseUrl = "/v1/agents"

export const agentService = {
  getList: (params: GetAgentRequestModel) =>
    api
      .get<ApiResponse<PagedResponse<AgentModel>>>(baseUrl, { params })
      .then((res) => unwrapApiResponse(res.data)),
  getById: (agentId: string) =>
    api
      .get<ApiResponse<AgentModel>>(`${baseUrl}/${agentId}`)
      .then((res) => unwrapApiResponse(res.data)),
  createAgent: (payload: CreateAgentModel) =>
    api
      .post<ApiResponse<AgentModel>>(baseUrl, payload)
      .then((res) => unwrapApiResponse(res.data)),
  updateAgent: (agentId: string, payload: UpdateAgentModel) =>
    api
      .put<ApiResponse<AgentModel>>(`${baseUrl}/${agentId}`, payload)
      .then((res) => unwrapApiResponse(res.data)),
  deleteAgent: (agentId: string) => api.delete(`${baseUrl}/${agentId}`),
}
