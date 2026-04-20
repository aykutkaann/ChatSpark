import { useNavigate } from "react-router-dom";
import type { WorkspaceResponse } from "../../types/workspace";

interface Props {
  workspaces: WorkspaceResponse[];
  onCreateClick: () => void;
  onJoinClick: () => void;
}

export function WorkspaceList({ workspaces, onCreateClick, onJoinClick }: Props) {
  const navigate = useNavigate();

  return (
    <div className="workspace-list-container">
      <div className="workspace-list-header">
        <h2>Your workspaces</h2>
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
              <div className="workspace-card-arrow">→</div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
