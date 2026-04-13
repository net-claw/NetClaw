import type {
  CreateSkillModel,
  GetSkillRequestModel,
  SkillModel,
  UpdateSkillModel,
} from "@/@types/models"
import type { ApiResponse, PagedResponse } from "@/@types/models/common"
import { api } from "@/lib/api"
import { unwrapApiResponse } from "@/lib/api-response"

const baseUrl = "/v1/llm/skills"

export const skillService = {
  getList: (params: GetSkillRequestModel) =>
    api
      .get<ApiResponse<PagedResponse<SkillModel>>>(baseUrl, { params })
      .then((res) => unwrapApiResponse(res.data)),
  getById: (skillId: string) =>
    api
      .get<ApiResponse<SkillModel>>(`${baseUrl}/${skillId}`)
      .then((res) => unwrapApiResponse(res.data)),
  createSkill: (payload: CreateSkillModel) =>
    api
      .post<ApiResponse<SkillModel>>(baseUrl, payload)
      .then((res) => unwrapApiResponse(res.data)),
  updateSkill: (skillId: string, payload: UpdateSkillModel) =>
    api
      .put<ApiResponse<SkillModel>>(`${baseUrl}/${skillId}`, payload)
      .then((res) => unwrapApiResponse(res.data)),
  installSkill: (skillId: string) =>
    api
      .post<ApiResponse<SkillModel>>(`${baseUrl}/${skillId}/install`)
      .then((res) => unwrapApiResponse(res.data)),
  deleteSkill: (skillId: string) => api.delete(`${baseUrl}/${skillId}`),
  uploadSkill: (file: File) => {
    const form = new FormData()
    form.append("file", file)

    return api
      .post<ApiResponse<SkillModel>>(`${baseUrl}/upload`, form, {
        headers: { "Content-Type": "multipart/form-data" },
      })
      .then((res) => unwrapApiResponse(res.data))
  },
}
