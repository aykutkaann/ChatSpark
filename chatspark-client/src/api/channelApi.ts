import api from "./axios";
import type { CreateChannelRequest, ChannelResponse } from "../types/channel";

export const channelApi = {
  getChannels: (workspaceId: string) =>
    api.get<ChannelResponse[]>(`/api/workspaces/${workspaceId}/channels`),

  createChannel: (workspaceId: string, data: CreateChannelRequest) =>
    api.post<ChannelResponse>(`/api/workspaces/${workspaceId}/channels`, data),

  deleteChannel: (workspaceId: string, channelId: string) =>
    api.delete(`/api/workspaces/${workspaceId}/channels/${channelId}`),

  getInviteCode: (workspaceId: string, channelId: string) =>
    api.get<{ inviteCode: string }>(`/api/workspaces/${workspaceId}/channels/${channelId}/invite-code`),

  joinByCode: (workspaceId: string, inviteCode: string) =>
    api.post<ChannelResponse>(`/api/workspaces/${workspaceId}/channels/join-by-code`, { inviteCode }),

  archiveChannel: (workspaceId: string, channelId: string) =>
    api.post(`/api/workspaces/${workspaceId}/channels/${channelId}/archive`),

  unarchiveChannel: (workspaceId: string, channelId: string) =>
    api.post(`/api/workspaces/${workspaceId}/channels/${channelId}/unarchive`),

  getPresence: (workspaceId: string, channelId: string) =>
    api.get<string[]>(`/api/workspaces/${workspaceId}/channels/${channelId}/presence`),

  getReadReceipts: (workspaceId: string, channelId: string) =>
    api.get<Record<string, string>>(`/api/workspaces/${workspaceId}/channels/${channelId}/readreceipts`),
};
