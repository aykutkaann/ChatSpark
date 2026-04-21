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

  // After creation, if private, show invite code step
  const [createdChannel, setCreatedChannel] = useState<ChannelResponse | null>(null);
  const [inviteCode, setInviteCode] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setIsLoading(true);
    try {
      const res = await channelApi.createChannel(workspaceId, { name, isPrivate });
      if (isPrivate && res.data.inviteCode) {
        // Show invite code step before closing
        setCreatedChannel(res.data);
        setInviteCode(res.data.inviteCode);
      } else {
        onCreated(res.data);
      }
    } catch {
      setError("Channel name may already be taken.");
    } finally {
      setIsLoading(false);
    }
  };

  const handleCopy = async () => {
    if (!inviteCode) return;
    await navigator.clipboard.writeText(inviteCode);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const handleDone = () => {
    if (createdChannel) onCreated(createdChannel);
  };

  // Step 2: show invite code
  if (inviteCode && createdChannel) {
    return (
      <div className="modal-overlay" onClick={handleDone}>
        <div className="modal" onClick={(e) => e.stopPropagation()}>
          <div className="modal-header">
            <h2>Channel created!</h2>
          </div>

          <div className="invite-code-section">
            <p className="invite-code-label">
              🔒 <strong>#{createdChannel.name}</strong> is private. Share this invite code with people you want to add:
            </p>
            <div className="invite-code-box">
              <span className="invite-code-value">{inviteCode}</span>
              <button className="btn-primary btn-sm" onClick={handleCopy}>
                {copied ? "Copied!" : "Copy"}
              </button>
            </div>
            <p className="form-hint">Members can join by pasting this code in "Join private channel".</p>
          </div>

          <div className="modal-actions">
            <button className="btn-primary" onClick={handleDone}>
              Go to channel
            </button>
          </div>
        </div>
      </div>
    );
  }

  // Step 1: create form
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
