import { useState, useEffect, useCallback, useRef } from "react";
import { messageApi } from "../api/messageApi";
import type { MessageResponse } from "../types/message";
import { useSignalR } from "../context/SignalRContext";

export function useMessages(channelId: string | null) {
  const [messages, setMessages] = useState<MessageResponse[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [hasMore, setHasMore] = useState(true);
  const { connection } = useSignalR();
  const channelIdRef = useRef(channelId);
  channelIdRef.current = channelId;

  const fetchMessages = useCallback(async (id: string) => {
    setIsLoading(true);
    try {
      const res = await messageApi.getMessages(id);
      setMessages(res.data.reverse());
      setHasMore(res.data.length === 50);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    if (!channelId) {
      setMessages([]);
      return;
    }
    fetchMessages(channelId);
  }, [channelId, fetchMessages]);

  useEffect(() => {
    if (!connection) return;

    const onReceived = (msg: MessageResponse) => {
      if (msg.channelId !== channelIdRef.current) return;
      setMessages((prev) => [...prev, msg]);
    };

    const onEdited = (msg: MessageResponse) => {
      if (msg.channelId !== channelIdRef.current) return;
      setMessages((prev) => prev.map((m) => (m.id === msg.id ? msg : m)));
    };

    const onDeleted = (messageId: string) => {
      setMessages((prev) => prev.filter((m) => m.id !== messageId));
    };

    connection.on("MessageReceived", onReceived);
    connection.on("MessageEdited", onEdited);
    connection.on("MessageDeleted", onDeleted);

    return () => {
      connection.off("MessageReceived", onReceived);
      connection.off("MessageEdited", onEdited);
      connection.off("MessageDeleted", onDeleted);
    };
  }, [connection]);

  const loadMore = useCallback(async () => {
    if (!channelId || isLoading || !hasMore || messages.length === 0) return;
    const oldest = messages[0].sentAt;
    setIsLoading(true);
    try {
      const res = await messageApi.getMessages(channelId, oldest);
      const older = res.data.reverse();
      setMessages((prev) => [...older, ...prev]);
      setHasMore(older.length === 50);
    } finally {
      setIsLoading(false);
    }
  }, [channelId, isLoading, hasMore, messages]);

  const sendMessage = useCallback(async (content: string) => {
    if (!channelId) return;
    await messageApi.sendMessage(channelId, { content });
  }, [channelId]);

  const editMessage = useCallback(async (messageId: string, content: string) => {
    if (!channelId) return;
    await messageApi.editMessage(channelId, messageId, { content });
  }, [channelId]);

  const deleteMessage = useCallback(async (messageId: string) => {
    if (!channelId) return;
    await messageApi.deleteMessage(channelId, messageId);
  }, [channelId]);

  return { messages, isLoading, hasMore, loadMore, sendMessage, editMessage, deleteMessage };
}
