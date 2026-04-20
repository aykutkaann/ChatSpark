import { useState } from "react";
import type { MessageResponse } from "../../types/message";
import { formatTime, formatRelativeTime } from "../../utils/dateFormat";
import { useAuth } from "../../context/AuthContext";

interface Props {
  message: MessageResponse;
  onEdit: (messageId: string, content: string) => void;
  onDelete: (messageId: string) => void;
}

export function MessageItem({ message, onEdit, onDelete }: Props) {
  const { user } = useAuth();
  const isOwn = message.senderId === user?.id;
  const [isEditing, setIsEditing] = useState(false);
  const [editContent, setEditContent] = useState(message.content);
  const [showActions, setShowActions] = useState(false);

  const handleEditSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (editContent.trim() && editContent !== message.content) {
      onEdit(message.id, editContent.trim());
    }
    setIsEditing(false);
  };

  return (
    <div
      className={`message-item ${isOwn ? "message-own" : ""}`}
      onMouseEnter={() => setShowActions(true)}
      onMouseLeave={() => setShowActions(false)}
    >
      <div className="message-avatar">
        {message.senderId.charAt(0).toUpperCase()}
      </div>

      <div className="message-content">
        <div className="message-meta">
          <span className="message-sender">{isOwn ? "You" : `User ${message.senderId.slice(0, 8)}`}</span>
          <span className="message-time" title={formatRelativeTime(message.sentAt)}>
            {formatTime(message.sentAt)}
          </span>
          {message.editedAt && <span className="message-edited">(edited)</span>}
        </div>

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
        ) : (
          <p className="message-text">{message.content}</p>
        )}
      </div>

      {isOwn && showActions && !isEditing && (
        <div className="message-actions">
          <button
            className="message-action-btn"
            onClick={() => { setIsEditing(true); setEditContent(message.content); }}
            title="Edit"
          >
            ✏️
          </button>
          <button
            className="message-action-btn message-action-danger"
            onClick={() => onDelete(message.id)}
            title="Delete"
          >
            🗑️
          </button>
        </div>
      )}
    </div>
  );
}
