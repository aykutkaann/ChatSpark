import { useState, useRef } from "react";
import type { ChannelResponse } from "../../types/channel";

interface Props {
  channel: ChannelResponse;
  onSend: (content: string) => Promise<void>;
  onTyping: () => void;
  onStopTyping: () => void;
}

export function MessageInput({ channel, onSend, onTyping, onStopTyping }: Props) {
  const [content, setContent] = useState("");
  const [isSending, setIsSending] = useState(false);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  const handleSubmit = async (e?: React.FormEvent) => {
    e?.preventDefault();
    const trimmed = content.trim();
    if (!trimmed || isSending) return;

    setIsSending(true);
    setContent("");
    onStopTyping();
    try {
      await onSend(trimmed);
    } finally {
      setIsSending(false);
    setTimeout(() => textareaRef.current?.focus(), 0);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      handleSubmit();
    }
  };

  const handleChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    setContent(e.target.value);
    if (e.target.value) onTyping();
    // Auto-resize
    e.target.style.height = "auto";
    e.target.style.height = `${Math.min(e.target.scrollHeight, 200)}px`;
  };

  return (
    <form className="message-input-form" onSubmit={handleSubmit}>
      <div className="message-input-wrapper">
        <textarea
          ref={textareaRef}
          className="message-input"
          value={content}
          onChange={handleChange}
          onKeyDown={handleKeyDown}
          placeholder={`Message #${channel.name}`}
          rows={1}
          disabled={isSending}
        />
        <button
          type="submit"
          className="message-send-btn"
          disabled={!content.trim() || isSending}
          title="Send message"
        >
          ↑
        </button>
      </div>
      <p className="message-input-hint">Press Enter to send, Shift+Enter for new line</p>
    </form>
  );
}
