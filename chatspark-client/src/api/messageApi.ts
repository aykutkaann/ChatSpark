import api from "./axios";
import type { MessageResponse, SendMessageRequest, EditMessageRequest } from "../types/message";

export const messageApi = {
  getMessages: (channelId: string, before?: string, limit = 50) =>
    api.get<MessageResponse[]>(`/api/channels/${channelId}/messages`, {
      params: { before, limit },
    }),

  sendMessage: (channelId: string, data: SendMessageRequest) =>
    api.post<MessageResponse>(`/api/channels/${channelId}/messages`, data),

  editMessage: (channelId: string, messageId: string, data: EditMessageRequest) =>
    api.patch<MessageResponse>(`/api/channels/${channelId}/messages/${messageId}`, data),

  deleteMessage: (channelId: string, messageId: string) =>
    api.delete(`/api/channels/${channelId}/messages/${messageId}`),

  uploadMedia: (channelId: string, file: File) => {
    const formData = new FormData();
    formData.append("file", file);
    return api.post<MessageResponse>(`/api/channels/${channelId}/messages/upload`, formData, {
      headers: { "Content-Type": "multipart/form-data" },
    });
  },
};
