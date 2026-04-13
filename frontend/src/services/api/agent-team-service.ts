import type {
  AgentTeamModel,
  CreateAgentTeamModel,
  GetAgentTeamRequestModel,
  UpdateAgentTeamModel,
} from "@/@types/models"
import type { ApiResponse, PagedResponse } from "@/@types/models/common"
import { api } from "@/lib/api"
import { unwrapApiResponse } from "@/lib/api-response"

const baseUrl = "/v1/agent-teams"

export const agentTeamService = {
  getList: (params: GetAgentTeamRequestModel) =>
    api
      .get<ApiResponse<PagedResponse<AgentTeamModel>>>(baseUrl, { params })
      .then((res) => unwrapApiResponse(res.data)),
  getById: (teamId: string) =>
    api
      .get<ApiResponse<AgentTeamModel>>(`${baseUrl}/${teamId}`)
      .then((res) => unwrapApiResponse(res.data)),
  createAgentTeam: (payload: CreateAgentTeamModel) =>
    api
      .post<ApiResponse<AgentTeamModel>>(baseUrl, payload)
      .then((res) => unwrapApiResponse(res.data)),
  updateAgentTeam: (teamId: string, payload: UpdateAgentTeamModel) =>
    api
      .put<ApiResponse<AgentTeamModel>>(`${baseUrl}/${teamId}`, payload)
      .then((res) => unwrapApiResponse(res.data)),
  deleteAgentTeam: (teamId: string) => api.delete(`${baseUrl}/${teamId}`),
}
