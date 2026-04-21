export interface ProfileResponse {
  id: string;
  email: string;
  displayName: string;
  avatarUrl?: string;
  bio?: string;
  websiteUrl?: string;
  createdAt: string;
}

export interface PublicProfileResponse {
  id: string;
  displayName: string;
  avatarUrl?: string;
  bio?: string;
  websiteUrl?: string;
  createdAt: string;
}

export interface UpdateProfileRequest {
  displayName?: string;
  avatarUrl?: string;
  bio?: string;
  websiteUrl?: string;
}
