export interface CreateWorkspaceRequest {
  name: string;
  slug: string;
}

export interface JoinWorkspaceRequest {
  slug: string;
}

export interface WorkspaceResponse {
  id: string;
  name: string;
  slug: string;
  ownerId: string;
  createdAt: string;
}

export interface WorkspaceMemberInfo {
  userId: string;
  displayName: string;
  avatarUrl?: string;
  role: "Owner" | "Admin" | "Member";
  isOnline: boolean;
}
