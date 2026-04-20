import { createContext, useContext, useEffect, useRef, useState } from "react";
import type { ReactNode } from "react";
import * as signalR from "@microsoft/signalr";
import { getAccessToken } from "../utils/tokenStorage";
import { useAuth } from "./AuthContext";

interface SignalRContextType {
  connection: signalR.HubConnection | null;
  isConnected: boolean;
  joinChannel: (channelId: string) => Promise<void>;
  leaveChannel: (channelId: string) => Promise<void>;
  invoke: (method: string, ...args: unknown[]) => Promise<void>;
}

const SignalRContext = createContext<SignalRContextType | null>(null);

export function SignalRProvider({ children }: { children: ReactNode }) {
  const { isAuthenticated } = useAuth();
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const [isConnected, setIsConnected] = useState(false);

  useEffect(() => {
    if (!isAuthenticated) return;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${import.meta.env.VITE_API_URL}/hubs/chat`, {
        accessTokenFactory: () => getAccessToken() ?? "",
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    connectionRef.current = connection;

    connection.onreconnected(() => setIsConnected(true));
    connection.onreconnecting(() => setIsConnected(false));
    connection.onclose(() => setIsConnected(false));

    connection.start().then(() => setIsConnected(true)).catch(console.error);

    return () => {
      connection.stop();
      setIsConnected(false);
    };
  }, [isAuthenticated]);

  const joinChannel = async (channelId: string) => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      await connectionRef.current.invoke("JoinChannel", channelId);
    }
  };

  const leaveChannel = async (channelId: string) => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      await connectionRef.current.invoke("LeaveChannel", channelId);
    }
  };

  const invoke = async (method: string, ...args: unknown[]) => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      await connectionRef.current.invoke(method, ...args);
    }
  };

  return (
    <SignalRContext.Provider value={{ connection: connectionRef.current, isConnected, joinChannel, leaveChannel, invoke }}>
      {children}
    </SignalRContext.Provider>
  );
}

export function useSignalR() {
  const context = useContext(SignalRContext);
  if (!context) throw new Error("useSignalR must be used within SignalRProvider");
  return context;
}
