interface Props {
  onlineUsers: string[];
  currentUserId: string;
}

export function PresenceBar({ onlineUsers, currentUserId }: Props) {
  const others = onlineUsers.filter((id) => id !== currentUserId);

  if (others.length === 0) return null;

  return (
    <div className="presence-bar">
      <div className="presence-avatars">
        {others.slice(0, 5).map((userId) => (
          <div key={userId} className="presence-avatar" title={`User ${userId.slice(0, 8)}`}>
            {userId.charAt(0).toUpperCase()}
          </div>
        ))}
        {others.length > 5 && (
          <div className="presence-avatar presence-avatar-overflow">+{others.length - 5}</div>
        )}
      </div>
      <span className="presence-label">
        {others.length === 1 ? "1 other online" : `${others.length} others online`}
      </span>
    </div>
  );
}
