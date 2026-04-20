import { useState, useEffect, useRef, useCallback } from "react";
import { useSignalR } from "../context/SignalRContext";

export function useTyping(channelId: string | null) {
  const [typingUsers, setTypingUsers] = useState<string[]>([]);
  const { connection, invoke } = useSignalR();
  const isTypingRef = useRef(false);
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    if (!connection) return;

    const onStarted = (data: { userId: string; displayName?: string }) => {
      const id = data.userId ?? data;
      setTypingUsers((prev) => prev.includes(String(id)) ? prev : [...prev, String(id)]);
    };

    const onStopped = (data: { userId: string }) => {
      const id = data.userId ?? data;
      setTypingUsers((prev) => prev.filter((u) => u !== String(id)));
    };

    connection.on("UserStartedTyping", onStarted);
    connection.on("UserStoppedTyping", onStopped);

    return () => {
      connection.off("UserStartedTyping", onStarted);
      connection.off("UserStoppedTyping", onStopped);
    };
  }, [connection]);

  useEffect(() => {
    setTypingUsers([]);
  }, [channelId]);

  const notifyTyping = useCallback(() => {
    if (!channelId) return;

    if (!isTypingRef.current) {
      isTypingRef.current = true;
      invoke("StartTyping", channelId);
    }

    if (timerRef.current) clearTimeout(timerRef.current);

    timerRef.current = setTimeout(async () => {
      isTypingRef.current = false;
      await invoke("StopTyping", channelId);
    }, 3000);
  }, [channelId, invoke]);

  const stopTyping = useCallback(() => {
    if (!channelId || !isTypingRef.current) return;
    if (timerRef.current) clearTimeout(timerRef.current);
    isTypingRef.current = false;
    invoke("StopTyping", channelId);
  }, [channelId, invoke]);

  return { typingUsers, notifyTyping, stopTyping };
}
