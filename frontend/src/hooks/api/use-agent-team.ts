import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"

import type {
  CreateAgentTeamModel,
  GetAgentTeamRequestModel,
  UpdateAgentTeamModel,
} from "@/@types/models"
import { agentTeamService } from "@/services/api/agent-team-service"

export const AGENT_TEAM_QUERY_KEYS = {
  UseGetAgentTeamList: (params?: GetAgentTeamRequestModel) => [
    "UseGetAgentTeamList",
    ...Object.values(params || {}),
  ],
  UseGetAgentTeamById: (teamId: string) => ["UseGetAgentTeamById", teamId],
}

export const useGetAgentTeamList = (params: GetAgentTeamRequestModel) =>
  useQuery({
    queryKey: AGENT_TEAM_QUERY_KEYS.UseGetAgentTeamList(params),
    queryFn: () => agentTeamService.getList(params),
  })

export const useGetAgentTeamById = (teamId: string) =>
  useQuery({
    queryKey: AGENT_TEAM_QUERY_KEYS.UseGetAgentTeamById(teamId),
    queryFn: () => agentTeamService.getById(teamId),
    enabled: Boolean(teamId),
  })

export const useCreateAgentTeam = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (payload: CreateAgentTeamModel) =>
      agentTeamService.createAgentTeam(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["UseGetAgentTeamList"] })
    },
  })
}

export const useUpdateAgentTeam = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({
      teamId,
      payload,
    }: {
      teamId: string
      payload: UpdateAgentTeamModel
    }) => agentTeamService.updateAgentTeam(teamId, payload),
    onSuccess: (_result, variables) => {
      void queryClient.invalidateQueries({ queryKey: ["UseGetAgentTeamList"] })
      void queryClient.invalidateQueries({
        queryKey: AGENT_TEAM_QUERY_KEYS.UseGetAgentTeamById(variables.teamId),
      })
    },
  })
}

export const useDeleteAgentTeams = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (teamIds: string[]) => {
      const results = await Promise.allSettled(
        teamIds.map((teamId) => agentTeamService.deleteAgentTeam(teamId))
      )

      const failedCount = results.filter(
        (result) => result.status === "rejected"
      ).length

      if (failedCount > 0) {
        throw new Error(
          failedCount === teamIds.length
            ? "Delete agent teams failed."
            : `Deleted ${teamIds.length - failedCount}/${teamIds.length} agent teams.`
        )
      }

      return results
    },
    meta: { skipErrorToast: true },
    onSuccess: (_result, teamIds) => {
      void queryClient.invalidateQueries({ queryKey: ["UseGetAgentTeamList"] })
      toast.success(
        teamIds.length === 1
          ? "Agent team deleted."
          : `${teamIds.length} agent teams deleted.`
      )
    },
    onError: (error) => {
      toast.error(
        error instanceof Error ? error.message : "Delete agent teams failed."
      )
    },
  })
}
