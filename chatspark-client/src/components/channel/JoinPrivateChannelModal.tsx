import { useState } from "react";
import { channelApi } from "../../api/channelApi";
import type { ChannelResponse } from "../../types/channel";

interface Props {
  workspaceId: string;
  onClose: () => void;
  onJoined: (channel: ChannelResponse) => void;
}

export function JoinPrivateChannelModal({ workspaceId, onClose, onJoined }: Props) {
  const [code, setCode] = useState("");
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setIsLoading(true);
    try {
      const res = await channelApi.joinByCode(workspaceId, code.trim());
      onJoined(res.data);
    } catch (err: any) {
      const msg = err?.response?.data?.message;
      setError(msg ?? "Invalid invite code or you are already a member.");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Join private channel</h2>
          <button className="modal-close" onClick={onClose}>✕</button>
        </div>

        <form onSubmit={handleSubmit} className="modal-form">
          {error && <div className="auth-error">{error}</div>}

          <div className="form-group">
            <label>Invite code</label>
            <input
              type="text"
              value={code}
              onChange={(e) => setCode(e.target.value)}
              placeholder="e.g. a3f9bc12"
              required
              autoFocus
            />
            <span className="form-hint">Ask a channel member to share their invite code</span>
          </div>

          <div className="modal-actions">
            <button type="button" className="btn-secondary" onClick={onClose}>Cancel</button>
            <button type="submit" className="btn-primary" disabled={isLoading || !code.trim()}>
              {isLoading ? "Joining..." : "Join channel"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
