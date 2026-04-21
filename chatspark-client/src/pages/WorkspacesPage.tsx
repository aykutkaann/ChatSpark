import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { workspaceApi } from "../api/workspaceApi";
import type { WorkspaceResponse } from "../types/workspace";
import { WorkspaceList } from "../components/workspace/WorkspaceList";
import { CreateWorkspaceModal } from "../components/workspace/CreateWorkspaceModal";
import { JoinWorkspaceModal } from "../components/workspace/JoinWorkspaceModal";
import { useAuth } from "../context/AuthContext";
import "../styles/workspace.css";

export function WorkspacesPage() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [workspaces, setWorkspaces] = useState<WorkspaceResponse[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [showCreate, setShowCreate] = useState(false);
  const [showJoin, setShowJoin] = useState(false);

  useEffect(() => {
    workspaceApi.getWorkspaces()
      .then((res) => setWorkspaces(res.data))
      .finally(() => setIsLoading(false));
  }, []);


  const handleDeleteWorkspace = async (id: string, name: string) => {
    const confirmed = window.confirm(
      `Delete workspace ${name}? This will delete all channels and messages forever`
    );

    if(!confirmed) return;

    try{
      await workspaceApi.deleteWorkspace(id);
      setWorkspaces((prev) => prev.filter((w) => w.id !== id));
    }catch(error){
      console.error("Failed to delete workspace:", error);
    }
  };

  return (
    <div className="workspaces-page">
      <header className="workspaces-header">
        <span className="brand-name">ChatSpark</span>
        <div className="header-user">
          <span className="header-display-name">{user?.displayName}</span>
          <button className="btn-secondary btn-sm" onClick={logout}>Sign out</button>
        </div>
      </header>

      <main className="workspaces-main">
        {isLoading ? (
          <div className="centered">
            <div className="spinner" />
          </div>
        ) : (
          <WorkspaceList
            workspaces={workspaces}
            currentUser={user} 
            onCreateClick={() => setShowCreate(true)}
            onJoinClick={() => setShowJoin(true)}
            onDelete={handleDeleteWorkspace} 
          />
        )}
      </main>

      {showCreate && (
        <CreateWorkspaceModal
          onClose={() => setShowCreate(false)}
          onCreated={(ws) => {
            setWorkspaces((prev) => [ws, ...prev]);
            setShowCreate(false);
            navigate(`/workspaces/${ws.id}`);
          }}
        />
      )}

      {showJoin && (
        <JoinWorkspaceModal
          onClose={() => setShowJoin(false)}
          onJoined={(ws) => {
            setWorkspaces((prev) => [...prev, ws]);
            setShowJoin(false);
            navigate(`/workspaces/${ws.id}`);
          }}
        />
      )}
    </div>
  );
}
