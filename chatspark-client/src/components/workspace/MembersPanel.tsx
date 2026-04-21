import { useState, useEffect } from "react";
import { workspaceApi } from "../../api/workspaceApi";
import type { WorkspaceMemberInfo } from "../../types/workspace";
import { useAuth } from "../../context/AuthContext";

interface Props {
  workspaceId: string;
  isOpen: boolean;
}

export function MembersPanel({ workspaceId, isOpen }: Props) {
  const { user } = useAuth();
  const [members, setMembers] = useState<WorkspaceMemberInfo[]>([]);
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    if (!isOpen) return;
    setIsLoading(true);
    workspaceApi.getMembers(workspaceId)
      .then((res) => setMembers(res.data))
      .finally(() => setIsLoading(false));
  }, [isOpen, workspaceId]);

  if (!isOpen) return null;

  const online = members.filter((m) => m.isOnline);
  const offline = members.filter((m) => !m.isOnline);

  const renderMember = (m: WorkspaceMemberInfo) => {
    const isYou = m.userId === user?.id;
    const letter = m.displayName.charAt(0).toUpperCase();

    return (
      <div key={m.userId} className="member-row">
        <div className="member-avatar-wrap">
          {m.avatarUrl ? (
            <img src={m.avatarUrl} alt={m.displayName} className="member-avatar-img" />
          ) : (
            <div className="member-avatar-letter">{letter}</div>
          )}
          <span className={`member-presence-dot ${m.isOnline ? "dot-online" : "dot-offline"}`} />
        </div>
        <div className="member-info">
          <span className="member-name">
            {m.displayName}
            {isYou && <span className="member-you"> (you)</span>}
          </span>
          <span className="member-role">{m.role}</span>
        </div>
      </div>
    );
  };

  return (
    <aside className="members-panel">
      <div className="members-panel-header">
        <span>Members</span>
        <span className="members-count">{members.length}</span>
      </div>

      {isLoading ? (
        <div className="members-loading"><div className="spinner spinner-sm" /></div>
      ) : (
        <>
          {online.length > 0 && (
            <div className="members-section">
              <div className="members-section-label">Online — {online.length}</div>
              {online.map(renderMember)}
            </div>
          )}
          {offline.length > 0 && (
            <div className="members-section">
              <div className="members-section-label">Offline — {offline.length}</div>
              {offline.map(renderMember)}
            </div>
          )}
        </>
      )}
    </aside>
  );
}
