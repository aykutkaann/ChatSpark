import { useState } from "react";
import type { ChannelResponse } from "../../types/channel";
import type { WorkspaceResponse } from "../../types/workspace";
import { CreateChannelModal } from "./CreateChannelModal";
import { useAuth } from "../../context/AuthContext";

interface Props {
  workspace: WorkspaceResponse;
  channels: ChannelResponse[];
  activeChannelId: string | null;
  onSelectChannel: (channelId: string) => void;
  onChannelCreated: (channel: ChannelResponse) => void;
  onLeaveWorkspace: () => void;
  isConnected: boolean;
}

export function ChannelSidebar({
  workspace,
  channels,
  activeChannelId,
  onSelectChannel,
  onChannelCreated,
  onLeaveWorkspace,
  isConnected,
}: Props) {
  const { user, logout } = useAuth();
  const [showCreateChannel, setShowCreateChannel] = useState(false);
  const [showMenu, setShowMenu] = useState(false);

  return (
    <aside className="sidebar">
      <div className="sidebar-workspace-header" onClick={() => setShowMenu(!showMenu)}>
        <div className="sidebar-workspace-name">
          <span>{workspace.name}</span>
          <span className="chevron">{showMenu ? "▲" : "▼"}</span>
        </div>
        {showMenu && (
          <div className="workspace-dropdown">
            <button onClick={onLeaveWorkspace} className="dropdown-item dropdown-item-danger">
              Leave workspace
            </button>
          </div>
        )}
      </div>

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
            </li>
          ))}
        </ul>
      </div>

      <div className="sidebar-user-panel">
        <div className="user-avatar-small">
          {user?.displayName?.charAt(0).toUpperCase()}
        </div>
        <div className="user-info">
          <span className="user-name">{user?.displayName}</span>
          <span className={`user-status ${isConnected ? "status-online" : "status-offline"}`}>
            {isConnected ? "Online" : "Offline"}
          </span>
        </div>
        <button className="btn-icon" onClick={logout} title="Sign out">
          ⎋
        </button>
      </div>

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
    </aside>
  );
}
