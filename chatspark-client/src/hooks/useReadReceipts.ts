import { useState, useEffect, useCallback } from "react";
import { channelApi } from "../api/channelApi";
import { useSignalR } from "../context/SignalRContext";

export function useReadReceipts(workspaceId: string | null, channelId: string | null) {
  const [receipts, setReceipts] = useState<Record<string, string>>({});
  const { connection, invoke } = useSignalR();

  useEffect(() => {
    if (!workspaceId || !channelId) {
      setReceipts({});
      return;
    }

    channelApi.getReadReceipts(workspaceId, channelId)
      .then((res) => setReceipts(res.data))
      .catch(() => setReceipts({}));
  }, [workspaceId, channelId]);

  useEffect(() => {
    if (!connection) return;

    const onRead = (data: { userId: string; messageId: string }) => {
      setReceipts((prev) => ({ ...prev, [data.userId]: data.messageId }));
    };

    connection.on("MessageRead", onRead);
    return () => connection.off("MessageRead", onRead);
  }, [connection]);

  const markAsRead = useCallback(async (messageId: string) => {
    if (!channelId) return;
    await invoke("MarkAsRead", channelId, messageId);
  }, [channelId, invoke]);

  return { receipts, markAsRead };
}
