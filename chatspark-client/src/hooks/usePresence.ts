import { useState, useEffect } from "react";
import { channelApi } from "../api/channelApi";
import { useSignalR } from "../context/SignalRContext";

export function usePresence(workspaceId: string | null, channelId: string | null) {
  const [onlineUsers, setOnlineUsers] = useState<string[]>([]);
  const { connection } = useSignalR();

  useEffect(() => {
    if (!workspaceId || !channelId) {
      setOnlineUsers([]);
      return;
    }

    channelApi.getPresence(workspaceId, channelId)
      .then((res) => setOnlineUsers(res.data))
      .catch(() => setOnlineUsers([]));
  }, [workspaceId, channelId]);

  useEffect(() => {
    if (!connection) return;

    const onOnline = (data: { userId: string } | string) => {
      const userId = typeof data === "string" ? data : data.userId;
      setOnlineUsers((prev) => prev.includes(userId) ? prev : [...prev, userId]);
    };

    const onOffline = (data: { userId: string } | string) => {
      const userId = typeof data === "string" ? data : data.userId;
      setOnlineUsers((prev) => prev.filter((id) => id !== userId));
    };

    connection.on("UserOnline", onOnline);
    connection.on("UserOffline", onOffline);

    return () => {
      connection.off("UserOnline", onOnline);
      connection.off("UserOffline", onOffline);
    };
  }, [connection]);

  return { onlineUsers };
}
