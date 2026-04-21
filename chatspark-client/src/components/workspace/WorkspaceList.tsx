import { useNavigate } from "react-router-dom";
import type { WorkspaceResponse } from "../../types/workspace";
import type { CurrentUser } from "../../types/auth";

interface Props {
  workspaces: WorkspaceResponse[];
  currentUser: CurrentUser | null;
  onCreateClick: () => void;
  onJoinClick: () => void;
  onDelete: (id: string, name: string) => void;
}

export function WorkspaceList({ workspaces, currentUser, onCreateClick, onJoinClick, onDelete }: Props) {
  const navigate = useNavigate();

  return (
    <div className="workspace-list-container">
      <div className="workspace-list-header">
        <h2>Your Workspaces</h2>
        <div className="workspace-actions">
          <button className="btn-secondary" onClick={onJoinClick}>Join</button>
          <button className="btn-primary" onClick={onCreateClick}>New workspace</button>
        </div>
      </div>

      {workspaces.length === 0 ? (
        <div className="empty-state">
          <div className="empty-state-icon">💬</div>
          <h3>No workspaces yet</h3>
          <p>Create a new workspace or join an existing one to get started.</p>
          <div className="empty-state-actions">
            <button className="btn-primary" onClick={onCreateClick}>Create workspace</button>
            <button className="btn-secondary" onClick={onJoinClick}>Join workspace</button>
          </div>
        </div>
      ) : (
        <div className="workspace-grid">
          {workspaces.map((ws) => (
            <div
              key={ws.id}
              className="workspace-card"
              onClick={() => navigate(`/workspaces/${ws.id}`)}
            >
              <div className="workspace-card-avatar">
                {ws.name.charAt(0).toUpperCase()}
              </div>
              <div className="workspace-card-info">
                <h3>{ws.name}</h3>
                <span className="workspace-slug">{ws.slug}</span>
              </div>

              {ws.ownerId === currentUser?.id && (
                <button
                  className="workspace-card-delete"
                  title="Delete workspace"
                  onClick={(e) => {
                    e.stopPropagation();
                    onDelete(ws.id, ws.name);
                  }}
                >
                  🗑
                </button>
              )}

              <div className="workspace-card-arrow">→</div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
