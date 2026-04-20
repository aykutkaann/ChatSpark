export interface SendMessageRequest {
  content: string;
}

export interface EditMessageRequest {
  content: string;
}

export interface MessageResponse {
  id: string;
  channelId: string;
  senderId: string;
  content: string;
  sentAt: string;
  editedAt: string | null;
  deletedAt: string | null;
}
