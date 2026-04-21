import api from "./axios";
import type { ProfileResponse, PublicProfileResponse, UpdateProfileRequest } from "../types/profile";

export const profileApi = {
  getProfile: () =>
    api.get<ProfileResponse>("/api/profile"),

  updateProfile: (data: UpdateProfileRequest) =>
    api.patch<ProfileResponse>("/api/profile", data),

  getUserProfile: (userId: string) =>
    api.get<PublicProfileResponse>(`/api/users/${userId}`),

  uploadAvatar: async (file: File): Promise<string> => {
    const form = new FormData();
    form.append("file", file);
    const res = await api.post<{ avatarUrl: string }>("/api/profile/avatar", form, {
      headers: { "Content-Type": "multipart/form-data" },
    });
    // Backend returns a relative path like /uploads/abc.jpg — prepend the API base URL
    return `${import.meta.env.VITE_API_URL}${res.data.avatarUrl}`;
  },
};
