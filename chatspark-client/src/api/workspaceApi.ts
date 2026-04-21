import api from "./axios";
import type { CreateWorkspaceRequest, JoinWorkspaceRequest, WorkspaceResponse, WorkspaceMemberInfo } from "../types/workspace";

export const workspaceApi = {
  getWorkspaces: () =>
    api.get<WorkspaceResponse[]>("/api/workspaces"),

  createWorkspace: (data: CreateWorkspaceRequest) =>
    api.post<WorkspaceResponse>("/api/workspaces", data),

  joinWorkspace: (data: JoinWorkspaceRequest) =>
    api.post<WorkspaceResponse>("/api/workspaces/join", data),

  leaveWorkspace: (workspaceId: string) =>
    api.post(`/api/workspaces/${workspaceId}/leave`),

  deleteWorkspace: (workspaceId: string) =>
    api.delete(`/api/workspaces/${workspaceId}`),

  getMembers: (workspaceId: string) =>
    api.get<WorkspaceMemberInfo[]>(`/api/workspaces/${workspaceId}/members`),
};
