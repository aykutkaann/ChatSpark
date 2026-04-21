import { useState } from "react";
import type { MessageResponse } from "../../types/message";
import { formatTime, formatRelativeTime } from "../../utils/dateFormat";
import { useAuth } from "../../context/AuthContext";
import { UserProfileModal } from "../profile/UserProfileModal";

interface Props {
  message: MessageResponse;
  showUsername: boolean;
  onEdit: (messageId: string, content: string) => void;
  onDelete: (messageId: string) => void;
}

export function MessageItem({ message, showUsername, onEdit, onDelete }: Props) {
  const { user } = useAuth();
  const isOwn = message.senderId === user?.id;
  const [isEditing, setIsEditing] = useState(false);
  const [editContent, setEditContent] = useState(message.content);
  const [showActions, setShowActions] = useState(false);
  const [showProfile, setShowProfile] = useState(false);

  const handleEditSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (editContent.trim() && editContent !== message.content) {
      onEdit(message.id, editContent.trim());
    }
    setIsEditing(false);
  };

  const avatarLetter = message.senderName.charAt(0).toUpperCase();
  const displayName = isOwn ? "You" : message.senderName;

  return (
    <div
      className={`message-item ${isOwn ? "message-own" : ""}`}
      onMouseEnter={() => setShowActions(true)}
      onMouseLeave={() => setShowActions(false)}
    >
      <div
        className="message-avatar"
        onClick={() => setShowProfile(true)}
        style={{ cursor: "pointer" }}
        title={`View ${displayName}'s profile`}
      >
        {message.senderAvatarUrl ? (
          <img src={message.senderAvatarUrl} alt={avatarLetter} className="message-avatar-img" />
        ) : (
          avatarLetter
        )}
      </div>

      <div className="message-content">
        {showUsername && (
          <div className="message-meta">
            <span
              className="message-sender"
              onClick={() => setShowProfile(true)}
              style={{ cursor: "pointer" }}
            >
              {displayName}
            </span>
            <span className="message-time" title={formatRelativeTime(message.sentAt)}>
              {formatTime(message.sentAt)}
            </span>
            {message.editedAt && <span className="message-edited">(edited)</span>}
          </div>
        )}

        {!showUsername && (
          <div className="message-meta message-meta-compact">
            <span className="message-time message-time-hidden" title={formatRelativeTime(message.sentAt)}>
              {formatTime(message.sentAt)}
            </span>
            {message.editedAt && <span className="message-edited">(edited)</span>}
          </div>
        )}

        {isEditing ? (
          <form onSubmit={handleEditSubmit} className="edit-form">
            <input
              type="text"
              value={editContent}
              onChange={(e) => setEditContent(e.target.value)}
              autoFocus
              className="edit-input"
            />
            <div className="edit-actions">
              <button type="submit" className="btn-primary btn-sm">Save</button>
              <button type="button" className="btn-secondary btn-sm" onClick={() => setIsEditing(false)}>Cancel</button>
            </div>
          </form>
        ) : message.messageType === 1 && message.fileUrl ? (
          <a
            href={`${import.meta.env.VITE_API_URL}${message.fileUrl}`}
            target="_blank"
            rel="noopener noreferrer"
            title="Open full size"
          >
            <img
              src={`${import.meta.env.VITE_API_URL}${message.fileUrl}`}
              alt="image"
              className="message-image"
              loading="lazy"
            />
          </a>
        ) : message.messageType === 2 && message.fileUrl ? (
          <audio
            controls
            src={`${import.meta.env.VITE_API_URL}${message.fileUrl}`}
            className="message-audio"
          />
        ) : (
          <p className="message-text">{message.content}</p>
        )}
      </div>

      {isOwn && showActions && !isEditing && (
        <div className="message-actions">
          {message.messageType === 0 && (
            <button
              className="message-action-btn"
              onClick={() => { setIsEditing(true); setEditContent(message.content); }}
              title="Edit"
            >
              ✏️
            </button>
          )}
          <button
            className="message-action-btn message-action-danger"
            onClick={() => onDelete(message.id)}
            title="Delete"
          >
            🗑️
          </button>
        </div>
      )}

      {showProfile && (
        <UserProfileModal
          userId={message.senderId}
          onClose={() => setShowProfile(false)}
        />
      )}
    </div>
  );
}
