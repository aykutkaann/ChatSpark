interface Props {
  typingUsers: string[];
}

export function TypingIndicator({ typingUsers }: Props) {
  if (typingUsers.length === 0) return null;

  const label =
    typingUsers.length === 1
      ? "Someone is typing"
      : `${typingUsers.length} people are typing`;

  return (
    <div className="typing-indicator">
      <div className="typing-dots">
        <span />
        <span />
        <span />
      </div>
      <span className="typing-label">{label}</span>
    </div>
  );
}
