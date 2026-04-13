import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"

import type {
  CreateSkillModel,
  GetSkillRequestModel,
  UpdateSkillModel,
} from "@/@types/models"
import { skillService } from "@/services/api/skill-service"

export const SKILL_QUERY_KEYS = {
  UseGetSkillList: (params?: GetSkillRequestModel) => [
    "UseGetSkillList",
    ...Object.values(params || {}),
  ],
  UseGetSkillById: (skillId: string) => ["UseGetSkillById", skillId],
}

export const useGetSkillList = (params: GetSkillRequestModel) =>
  useQuery({
    queryKey: SKILL_QUERY_KEYS.UseGetSkillList(params),
    queryFn: () => skillService.getList(params),
  })

export const useGetSkillById = (skillId: string) =>
  useQuery({
    queryKey: SKILL_QUERY_KEYS.UseGetSkillById(skillId),
    queryFn: () => skillService.getById(skillId),
    enabled: Boolean(skillId),
  })

export const useCreateSkill = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (payload: CreateSkillModel) => skillService.createSkill(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["UseGetSkillList"] })
    },
  })
}

export const useUpdateSkill = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({
      skillId,
      payload,
    }: {
      skillId: string
      payload: UpdateSkillModel
    }) => skillService.updateSkill(skillId, payload),
    onSuccess: (_result, variables) => {
      void queryClient.invalidateQueries({ queryKey: ["UseGetSkillList"] })
      void queryClient.invalidateQueries({
        queryKey: SKILL_QUERY_KEYS.UseGetSkillById(variables.skillId),
      })
    },
  })
}

export const useDeleteSkills = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (skillIds: string[]) => {
      const results = await Promise.allSettled(
        skillIds.map((skillId) => skillService.deleteSkill(skillId))
      )

      const failedCount = results.filter(
        (result) => result.status === "rejected"
      ).length

      if (failedCount > 0) {
        throw new Error(
          failedCount === skillIds.length
            ? "Delete skills failed."
            : `Deleted ${skillIds.length - failedCount}/${skillIds.length} skills.`
        )
      }

      return results
    },
    meta: { skipErrorToast: true },
    onSuccess: (_result, skillIds) => {
      void queryClient.invalidateQueries({ queryKey: ["UseGetSkillList"] })
      toast.success(
        skillIds.length === 1
          ? "Skill deleted."
          : `${skillIds.length} skills deleted.`
      )
    },
    onError: (error) => {
      toast.error(
        error instanceof Error ? error.message : "Delete skills failed."
      )
    },
  })
}

export const useUploadSkill = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (file: File) => skillService.uploadSkill(file),
    onSuccess: (skill) => {
      void queryClient.invalidateQueries({ queryKey: ["UseGetSkillList"] })
      void queryClient.invalidateQueries({
        queryKey: SKILL_QUERY_KEYS.UseGetSkillById(skill.id),
      })
    },
  })
}

export const useInstallSkill = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (skillId: string) => skillService.installSkill(skillId),
    onSuccess: (skill) => {
      void queryClient.invalidateQueries({ queryKey: ["UseGetSkillList"] })
      void queryClient.invalidateQueries({
        queryKey: SKILL_QUERY_KEYS.UseGetSkillById(skill.id),
      })
      toast.success("Skill installed.")
    },
    onError: (error) => {
      toast.error(
        error instanceof Error ? error.message : "Skill install failed."
      )
    },
  })
}
