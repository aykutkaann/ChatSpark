import api from "./axios";
import type { ProfileResponse, UpdateProfileRequest } from "../types/profile";

export const profileApi = {
  getProfile: () =>
    api.get<ProfileResponse>("/api/profile"),

  updateProfile: (data: UpdateProfileRequest) =>
    api.patch<ProfileResponse>("/api/profile", data),
};
