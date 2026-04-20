import api from "./axios";
import type { CreateWorkspaceRequest, JoinWorkspaceRequest, WorkspaceResponse } from "../types/workspace";

export const workspaceApi = {
  getWorkspaces: () =>
    api.get<WorkspaceResponse[]>("/api/workspaces"),

  createWorkspace: (data: CreateWorkspaceRequest) =>
    api.post<WorkspaceResponse>("/api/workspaces", data),

  joinWorkspace: (data: JoinWorkspaceRequest) =>
    api.post<WorkspaceResponse>("/api/workspaces/join", data),

  leaveWorkspace: (workspaceId: string) =>
    api.post(`/api/workspaces/${workspaceId}/leave`),
};
