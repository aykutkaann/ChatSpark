export interface CreateChannelRequest {
  name: string;
  isPrivate: boolean;
}

export interface ChannelResponse {
  id: string;
  workspaceId: string;
  name: string;
  isPrivate: boolean;
  isArchived: boolean;
  createdAt: string;
  inviteCode?: string;
}
