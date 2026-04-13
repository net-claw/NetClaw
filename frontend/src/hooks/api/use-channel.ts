import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"

import type {
  CreateChannelModel,
  GetChannelRequestModel,
  UpdateChannelModel,
} from "@/@types/models"
import { channelService } from "@/services/api/channel-service"

export const CHANNEL_QUERY_KEYS = {
  UseGetChannelById: (channelId: string) => ["UseGetChannelById", channelId],
  UseGetChannelList: (params?: GetChannelRequestModel) => [
    "UseGetChannelList",
    ...Object.values(params || {}),
  ],
}

export const useGetChannelById = (channelId: string) =>
  useQuery({
    queryKey: CHANNEL_QUERY_KEYS.UseGetChannelById(channelId),
    queryFn: () => channelService.getById(channelId),
    enabled: Boolean(channelId),
  })

export const useGetChannelList = (params: GetChannelRequestModel) =>
  useQuery({
    queryKey: CHANNEL_QUERY_KEYS.UseGetChannelList(params),
    queryFn: () => channelService.getList(params),
  })

export const useCreateChannel = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (payload: CreateChannelModel) =>
      channelService.createChannel(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["UseGetChannelList"] })
    },
  })
}

export const useUpdateChannel = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({
      channelId,
      payload,
    }: {
      channelId: string
      payload: UpdateChannelModel
    }) => channelService.updateChannel(channelId, payload),
    onSuccess: (_result, { channelId }) => {
      void queryClient.invalidateQueries({ queryKey: ["UseGetChannelList"] })
      void queryClient.invalidateQueries({
        queryKey: CHANNEL_QUERY_KEYS.UseGetChannelById(channelId),
      })
    },
  })
}

export const useDeleteChannel = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (channelId: string) => channelService.deleteChannel(channelId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["UseGetChannelList"] })
    },
  })
}

export const useDeleteChannels = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (channelIds: string[]) => {
      const results = await Promise.allSettled(
        channelIds.map((channelId) => channelService.deleteChannel(channelId))
      )

      const failedCount = results.filter(
        (result) => result.status === "rejected"
      ).length

      if (failedCount > 0) {
        throw new Error(
          failedCount === channelIds.length
            ? "Delete channels failed."
            : `Deleted ${channelIds.length - failedCount}/${channelIds.length} channels.`
        )
      }

      return results
    },
    meta: {
      skipErrorToast: true,
    },
    onSuccess: (_result, channelIds) => {
      void queryClient.invalidateQueries({ queryKey: ["UseGetChannelList"] })
      toast.success(
        channelIds.length === 1
          ? "Channel deleted."
          : `${channelIds.length} channels deleted.`
      )
    },
    onError: (error) => {
      toast.error(
        error instanceof Error ? error.message : "Delete channels failed."
      )
    },
  })
}

export const useStartChannel = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (channelId: string) => channelService.startChannel(channelId),
    onSuccess: (_result, channelId) => {
      void queryClient.invalidateQueries({ queryKey: ["UseGetChannelList"] })
      void queryClient.invalidateQueries({
        queryKey: CHANNEL_QUERY_KEYS.UseGetChannelById(channelId),
      })
    },
  })
}

export const useStopChannel = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (channelId: string) => channelService.stopChannel(channelId),
    onSuccess: (_result, channelId) => {
      void queryClient.invalidateQueries({ queryKey: ["UseGetChannelList"] })
      void queryClient.invalidateQueries({
        queryKey: CHANNEL_QUERY_KEYS.UseGetChannelById(channelId),
      })
    },
  })
}

export const useRestartChannel = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (channelId: string) => channelService.restartChannel(channelId),
    onSuccess: (_result, channelId) => {
      void queryClient.invalidateQueries({ queryKey: ["UseGetChannelList"] })
      void queryClient.invalidateQueries({
        queryKey: CHANNEL_QUERY_KEYS.UseGetChannelById(channelId),
      })
    },
  })
}
