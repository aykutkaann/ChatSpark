import { useState } from "react";
import { useNavigate } from "react-router-dom";
import type { ChannelResponse } from "../../types/channel";
import type { WorkspaceResponse } from "../../types/workspace";
import { CreateChannelModal } from "./CreateChannelModal";
import { JoinPrivateChannelModal } from "./JoinPrivateChannelModal";
import { ProfileSettingsModal } from "../profile/ProfileSettingsModal";
import { useAuth } from "../../context/AuthContext";
import { channelApi } from "../../api/channelApi";

interface Props {
  workspace: WorkspaceResponse;
  channels: ChannelResponse[];
  activeChannelId: string | null;
  onSelectChannel: (channelId: string) => void;
  onChannelCreated: (channel: ChannelResponse) => void;
  onChannelDeleted: (channelId: string) => void;
  onChannelJoined: (channel: ChannelResponse) => void;
  onLeaveWorkspace: () => void;
  onDeleteWorkspace: () => void;
  isConnected: boolean;
}

export function ChannelSidebar({
  workspace,
  channels,
  activeChannelId,
  onSelectChannel,
  onChannelCreated,
  onChannelDeleted,
  onChannelJoined,
  onLeaveWorkspace,
  onDeleteWorkspace,
  isConnected,
}: Props) {
  const navigate = useNavigate();
  const { user, logout } = useAuth();

  const [showMenu, setShowMenu] = useState(false);
  const [showCreateChannel, setShowCreateChannel] = useState(false);
  const [showJoinByCode, setShowJoinByCode] = useState(false);
  const [showProfile, setShowProfile] = useState(false);

  // Per-channel copy-to-clipboard state
  const [copyState, setCopyState] = useState<Record<string, "idle" | "copied">>({});

  const handleCopyInviteCode = async (e: React.MouseEvent, ch: ChannelResponse) => {
    e.stopPropagation();
    try {
      const res = await channelApi.getInviteCode(workspace.id, ch.id);
      await navigator.clipboard.writeText(res.data.inviteCode);
      setCopyState((prev) => ({ ...prev, [ch.id]: "copied" }));
      setTimeout(() => setCopyState((prev) => ({ ...prev, [ch.id]: "idle" })), 2000);
    } catch {
      alert("Could not copy invite code.");
    }
  };

  const handleDelete = async (e: React.MouseEvent, ch: ChannelResponse) => {
    e.stopPropagation();
    if (!confirm(`Delete #${ch.name}? This cannot be undone.`)) return;
    try {
      await channelApi.deleteChannel(workspace.id, ch.id);
      onChannelDeleted(ch.id);
    } catch (err: any) {
      if (err?.response?.status === 403) {
        alert("Only workspace admins and owners can delete channels.");
      } else {
        alert("Failed to delete channel.");
      }
    }
  };

  const avatarLetter = (user?.displayName ?? "?").charAt(0).toUpperCase();

  return (
    <aside className="sidebar">
      {/* Workspace header dropdown */}
      <div
        className="sidebar-workspace-header"
        onClick={() => setShowMenu((prev) => !prev)}
      >
        <div className="sidebar-workspace-name">
          <span>{workspace.name}</span>
          <span className="chevron">{showMenu ? "▲" : "▼"}</span>
        </div>

        {showMenu && (
          <div
            className="workspace-dropdown"
            onClick={(e) => e.stopPropagation()}
          >
            <button
              className="dropdown-item"
              onClick={() => { setShowMenu(false); navigate("/"); }}
            >
              ← All workspaces
            </button>
            {workspace.ownerId !== user?.id && (
              <button
                className="dropdown-item dropdown-item-danger"
                onClick={() => { setShowMenu(false); onLeaveWorkspace(); }}
              >
                Leave workspace
              </button>
            )}
            {workspace.ownerId === user?.id && (
              <button
                className="dropdown-item dropdown-item-danger"
                onClick={() => {
                  setShowMenu(false);
                  if (confirm(`Delete "${workspace.name}"? This will delete all channels and messages forever.`)) {
                    onDeleteWorkspace();
                  }
                }}
              >
                🗑 Delete workspace
              </button>
            )}
          </div>
        )}
      </div>

      {/* Channel list */}
      <div className="sidebar-section">
        <div className="sidebar-section-header">
          <span>Channels</span>
          <button
            className="sidebar-add-btn"
            onClick={() => setShowCreateChannel(true)}
            title="Add channel"
          >
            +
          </button>
        </div>

        <ul className="channel-list">
          {channels.map((ch) => (
            <li
              key={ch.id}
              className={`channel-item ${activeChannelId === ch.id ? "channel-item-active" : ""}`}
              onClick={() => onSelectChannel(ch.id)}
            >
              <span className="channel-icon">{ch.isPrivate ? "🔒" : "#"}</span>
              <span className="channel-name">{ch.name}</span>

              <div className="channel-actions">
                {ch.isPrivate && (
                  <button
                    className="channel-action-btn"
                    title="Copy invite code"
                    onClick={(e) => handleCopyInviteCode(e, ch)}
                  >
                    {copyState[ch.id] === "copied" ? "✓" : "🔗"}
                  </button>
                )}
                <button
                  className="channel-action-btn channel-action-danger"
                  title="Delete channel"
                  onClick={(e) => handleDelete(e, ch)}
                >
                  🗑
                </button>
              </div>
            </li>
          ))}
        </ul>

        <button
          className="join-private-btn"
          onClick={() => setShowJoinByCode(true)}
        >
          🔑 Join private channel
        </button>
      </div>

      {/* User panel */}
      <div className="sidebar-user-panel">
        <div className="user-avatar-small">
          {user?.avatarUrl ? (
            <img src={user.avatarUrl} alt="avatar" className="user-avatar-img" />
          ) : (
            avatarLetter
          )}
        </div>
        <div className="user-info">
          <span className="user-name">{user?.displayName}</span>
          <span className={`user-status ${isConnected ? "status-online" : "status-offline"}`}>
            {isConnected ? "Online" : "Offline"}
          </span>
        </div>
        <button
          className="btn-icon"
          onClick={() => setShowProfile(true)}
          title="Profile settings"
        >
          ⚙
        </button>
        <button className="btn-icon" onClick={logout} title="Sign out">
          ⎋
        </button>
      </div>

      {/* Modals */}
      {showCreateChannel && (
        <CreateChannelModal
          workspaceId={workspace.id}
          onClose={() => setShowCreateChannel(false)}
          onCreated={(ch) => {
            onChannelCreated(ch);
            setShowCreateChannel(false);
          }}
        />
      )}

      {showJoinByCode && (
        <JoinPrivateChannelModal
          workspaceId={workspace.id}
          onClose={() => setShowJoinByCode(false)}
          onJoined={(ch) => {
            onChannelJoined(ch);
            setShowJoinByCode(false);
          }}
        />
      )}

      {showProfile && (
        <ProfileSettingsModal onClose={() => setShowProfile(false)} />
      )}
    </aside>
  );
}
