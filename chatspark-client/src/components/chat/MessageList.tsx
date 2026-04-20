import { useEffect, useRef, useCallback } from "react";
import type { MessageResponse } from "../../types/message";
import { MessageItem } from "./MessageItem";
import { formatDate } from "../../utils/dateFormat";

interface Props {
  messages: MessageResponse[];
  isLoading: boolean;
  hasMore: boolean;
  onLoadMore: () => void;
  onEdit: (messageId: string, content: string) => void;
  onDelete: (messageId: string) => void;
}

function groupByDate(messages: MessageResponse[]) {
  const groups: { date: string; messages: MessageResponse[] }[] = [];
  let currentDate = "";

  for (const msg of messages) {
    const date = formatDate(msg.sentAt);
    if (date !== currentDate) {
      currentDate = date;
      groups.push({ date, messages: [msg] });
    } else {
      groups[groups.length - 1].messages.push(msg);
    }
  }
  return groups;
}

export function MessageList({ messages, isLoading, hasMore, onLoadMore, onEdit, onDelete }: Props) {
  const bottomRef = useRef<HTMLDivElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const isFirstLoad = useRef(true);
  const prevMessageCount = useRef(messages.length);

  useEffect(() => {
    if (isFirstLoad.current && messages.length > 0) {
      bottomRef.current?.scrollIntoView();
      isFirstLoad.current = false;
    } else if (messages.length > prevMessageCount.current) {
      const container = containerRef.current;
      if (container) {
        const isNearBottom = container.scrollHeight - container.scrollTop - container.clientHeight < 200;
        if (isNearBottom) bottomRef.current?.scrollIntoView({ behavior: "smooth" });
      }
    }
    prevMessageCount.current = messages.length;
  }, [messages.length]);

  const handleScroll = useCallback(() => {
    const container = containerRef.current;
    if (!container || isLoading || !hasMore) return;
    if (container.scrollTop < 100) onLoadMore();
  }, [isLoading, hasMore, onLoadMore]);

  const groups = groupByDate(messages);

  return (
    <div className="message-list" ref={containerRef} onScroll={handleScroll}>
      {isLoading && (
        <div className="message-list-loading">
          <div className="spinner spinner-sm" />
        </div>
      )}

      {!hasMore && messages.length > 0 && (
        <div className="message-list-start">
          <div className="message-list-start-line" />
          <span>Beginning of conversation</span>
          <div className="message-list-start-line" />
        </div>
      )}

      {groups.map((group) => (
        <div key={group.date}>
          <div className="date-divider">
            <div className="date-divider-line" />
            <span className="date-divider-label">{group.date}</span>
            <div className="date-divider-line" />
          </div>
          {group.messages.map((msg) => (
            <MessageItem key={msg.id} message={msg} onEdit={onEdit} onDelete={onDelete} />
          ))}
        </div>
      ))}

      {messages.length === 0 && !isLoading && (
        <div className="empty-channel">
          <p>No messages yet. Be the first to say something!</p>
        </div>
      )}

      <div ref={bottomRef} />
    </div>
  );
}
