import { useState } from "react";
import { channelApi } from "../../api/channelApi";
import type { ChannelResponse } from "../../types/channel";

interface Props {
  workspaceId: string;
  onClose: () => void;
  onCreated: (channel: ChannelResponse) => void;
}

export function CreateChannelModal({ workspaceId, onClose, onCreated }: Props) {
  const [name, setName] = useState("");
  const [isPrivate, setIsPrivate] = useState(false);
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setIsLoading(true);
    try {
      const res = await channelApi.createChannel(workspaceId, { name, isPrivate });
      onCreated(res.data);
    } catch {
      setError("Channel name may already be taken.");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Create a channel</h2>
          <button className="modal-close" onClick={onClose}>✕</button>
        </div>

        <form onSubmit={handleSubmit} className="modal-form">
          {error && <div className="auth-error">{error}</div>}

          <div className="form-group">
            <label>Channel name</label>
            <input
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value.toLowerCase().replace(/\s/g, "-"))}
              placeholder="general"
              required
              autoFocus
            />
          </div>

          <div className="form-group">
            <label className="checkbox-label">
              <input
                type="checkbox"
                checked={isPrivate}
                onChange={(e) => setIsPrivate(e.target.checked)}
              />
              <span>Make private</span>
            </label>
            <span className="form-hint">
              {isPrivate
                ? "Only invited members can see this channel"
                : "Anyone in the workspace can see this channel"}
            </span>
          </div>

          <div className="modal-actions">
            <button type="button" className="btn-secondary" onClick={onClose}>Cancel</button>
            <button type="submit" className="btn-primary" disabled={isLoading}>
              {isLoading ? "Creating..." : "Create channel"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
