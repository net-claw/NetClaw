import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"

import type {
  CreateAgentModel,
  GetAgentRequestModel,
  UpdateAgentModel,
} from "@/@types/models"
import { agentService } from "@/services/api/agent-service"

export const AGENT_QUERY_KEYS = {
  UseGetAgentList: (params?: GetAgentRequestModel) => [
    "UseGetAgentList",
    ...Object.values(params || {}),
  ],
  UseGetAgentById: (agentId: string) => ["UseGetAgentById", agentId],
}

export const useGetAgentList = (params: GetAgentRequestModel) =>
  useQuery({
    queryKey: AGENT_QUERY_KEYS.UseGetAgentList(params),
    queryFn: () => agentService.getList(params),
  })

export const useGetAgentById = (agentId: string) =>
  useQuery({
    queryKey: AGENT_QUERY_KEYS.UseGetAgentById(agentId),
    queryFn: () => agentService.getById(agentId),
    enabled: Boolean(agentId),
  })

export const useCreateAgent = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (payload: CreateAgentModel) => agentService.createAgent(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["UseGetAgentList"] })
    },
  })
}

export const useUpdateAgent = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({
      agentId,
      payload,
    }: {
      agentId: string
      payload: UpdateAgentModel
    }) => agentService.updateAgent(agentId, payload),
    onSuccess: (_result, variables) => {
      void queryClient.invalidateQueries({ queryKey: ["UseGetAgentList"] })
      void queryClient.invalidateQueries({
        queryKey: AGENT_QUERY_KEYS.UseGetAgentById(variables.agentId),
      })
    },
  })
}

export const useDeleteAgents = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (agentIds: string[]) => {
      const results = await Promise.allSettled(
        agentIds.map((agentId) => agentService.deleteAgent(agentId))
      )

      const failedCount = results.filter(
        (result) => result.status === "rejected"
      ).length

      if (failedCount > 0) {
        throw new Error(
          failedCount === agentIds.length
            ? "Delete agents failed."
            : `Deleted ${agentIds.length - failedCount}/${agentIds.length} agents.`
        )
      }

      return results
    },
    meta: { skipErrorToast: true },
    onSuccess: (_result, agentIds) => {
      void queryClient.invalidateQueries({ queryKey: ["UseGetAgentList"] })
      toast.success(
        agentIds.length === 1 ? "Agent deleted." : `${agentIds.length} agents deleted.`
      )
    },
    onError: (error) => {
      toast.error(
        error instanceof Error ? error.message : "Delete agents failed."
      )
    },
  })
}
