import { useState } from "react";
import { workspaceApi } from "../../api/workspaceApi";
import type { WorkspaceResponse } from "../../types/workspace";

interface Props {
  onClose: () => void;
  onJoined: (workspace: WorkspaceResponse) => void;
}

export function JoinWorkspaceModal({ onClose, onJoined }: Props) {
  const [slug, setSlug] = useState("");
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setIsLoading(true);
    try {
      const res = await workspaceApi.joinWorkspace({ slug });
      onJoined(res.data);
    } catch {
      setError("Workspace not found or you're already a member.");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Join a workspace</h2>
          <button className="modal-close" onClick={onClose}>✕</button>
        </div>

        <form onSubmit={handleSubmit} className="modal-form">
          {error && <div className="auth-error">{error}</div>}

          <div className="form-group">
            <label>Workspace slug</label>
            <input
              type="text"
              value={slug}
              onChange={(e) => setSlug(e.target.value)}
              placeholder="acme-corp"
              required
              autoFocus
            />
            <span className="form-hint">Ask the workspace owner for the slug</span>
          </div>

          <div className="modal-actions">
            <button type="button" className="btn-secondary" onClick={onClose}>Cancel</button>
            <button type="submit" className="btn-primary" disabled={isLoading}>
              {isLoading ? "Joining..." : "Join workspace"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
