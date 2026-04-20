import { useState, useEffect, useCallback } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { workspaceApi } from "../api/workspaceApi";
import { channelApi } from "../api/channelApi";
import type { WorkspaceResponse } from "../types/workspace";
import type { ChannelResponse } from "../types/channel";
import { ChannelSidebar } from "../components/channel/ChannelSidebar";
import { MessageList } from "../components/chat/MessageList";
import { MessageInput } from "../components/chat/MessageInput";
import { TypingIndicator } from "../components/chat/TypingIndicator";
import { PresenceBar } from "../components/chat/PresenceBar";
import { useMessages } from "../hooks/useMessages";
import { usePresence } from "../hooks/usePresence";
import { useTyping } from "../hooks/useTyping";
import { useReadReceipts } from "../hooks/useReadReceipts";
import { useSignalR } from "../context/SignalRContext";
import { useAuth } from "../context/AuthContext";
import "../styles/layout.css";
import "../styles/channel.css";
import "../styles/chat.css";

export function ChatPage() {
  const { workspaceId } = useParams<{ workspaceId: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const { joinChannel, leaveChannel, isConnected } = useSignalR();

  const [workspace, setWorkspace] = useState<WorkspaceResponse | null>(null);
  const [channels, setChannels] = useState<ChannelResponse[]>([]);
  const [activeChannelId, setActiveChannelId] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const activeChannel = channels.find((c) => c.id === activeChannelId) ?? null;

  const { messages, isLoading: messagesLoading, hasMore, loadMore, sendMessage, editMessage, deleteMessage } =
    useMessages(activeChannelId);

  const { onlineUsers } = usePresence(workspaceId ?? null, activeChannelId);
  const { typingUsers, notifyTyping, stopTyping } = useTyping(activeChannelId);
  const { markAsRead } = useReadReceipts(workspaceId ?? null, activeChannelId);

  useEffect(() => {
    if (!workspaceId) return;
    setIsLoading(true);

    Promise.all([
      workspaceApi.getWorkspaces(),
      channelApi.getChannels(workspaceId),
    ]).then(([wsRes, chRes]) => {
      const found = wsRes.data.find((w) => w.id === workspaceId);
      if (!found) { navigate("/"); return; }
      setWorkspace(found);
      setChannels(chRes.data);
      if (chRes.data.length > 0) setActiveChannelId(chRes.data[0].id);
    }).finally(() => setIsLoading(false));
  }, [workspaceId, navigate]);

  const handleSelectChannel = useCallback(async (channelId: string) => {
    if (activeChannelId) await leaveChannel(activeChannelId);
    setActiveChannelId(channelId);
    await joinChannel(channelId);
  }, [activeChannelId, joinChannel, leaveChannel]);

  useEffect(() => {
    if (activeChannelId) joinChannel(activeChannelId);
    return () => { if (activeChannelId) leaveChannel(activeChannelId); };
  }, [activeChannelId]);

  useEffect(() => {
    if (messages.length > 0) {
      const last = messages[messages.length - 1];
      markAsRead(last.id);
    }
  }, [messages.length]);

  const handleLeaveWorkspace = async () => {
    if (!workspaceId) return;
    try {
      await workspaceApi.leaveWorkspace(workspaceId);
      navigate("/");
    } catch {
      alert("Owners cannot leave the workspace.");
    }
  };

  if (isLoading) {
    return (
      <div className="splash-screen">
        <div className="spinner" />
      </div>
    );
  }

  if (!workspace) return null;

  return (
    <div className="chat-layout">
      <ChannelSidebar
        workspace={workspace}
        channels={channels}
        activeChannelId={activeChannelId}
        onSelectChannel={handleSelectChannel}
        onChannelCreated={(ch) => {
          setChannels((prev) => [...prev, ch]);
          handleSelectChannel(ch.id);
        }}
        onLeaveWorkspace={handleLeaveWorkspace}
        isConnected={isConnected}
      />

      <main className="chat-main">
        {activeChannel ? (
          <>
            <div className="chat-header">
              <div className="chat-header-left">
                <span className="chat-header-icon">{activeChannel.isPrivate ? "🔒" : "#"}</span>
                <h2 className="chat-header-name">{activeChannel.name}</h2>
              </div>
              <PresenceBar onlineUsers={onlineUsers} currentUserId={user?.id ?? ""} />
            </div>

            <MessageList
              messages={messages}
              isLoading={messagesLoading}
              hasMore={hasMore}
              onLoadMore={loadMore}
              onEdit={editMessage}
              onDelete={deleteMessage}
            />

            <div className="chat-footer">
              <TypingIndicator typingUsers={typingUsers} />
              <MessageInput
                channel={activeChannel}
                onSend={sendMessage}
                onTyping={notifyTyping}
                onStopTyping={stopTyping}
              />
            </div>
          </>
        ) : (
          <div className="no-channel">
            <p>Select a channel or create one to start chatting.</p>
          </div>
        )}
      </main>
    </div>
  );
}
