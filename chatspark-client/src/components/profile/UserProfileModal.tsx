import { useState, useEffect } from "react";
import { profileApi } from "../../api/profileApi";
import type { PublicProfileResponse } from "../../types/profile";

interface Props {
  userId: string;
  onClose: () => void;
}

export function UserProfileModal({ userId, onClose }: Props) {
  const [profile, setProfile] = useState<PublicProfileResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    setIsLoading(true);
    profileApi
      .getUserProfile(userId)
      .then((res) => setProfile(res.data))
      .catch(() => setProfile(null))
      .finally(() => setIsLoading(false));
  }, [userId]);

  const handleBackdrop = (e: React.MouseEvent<HTMLDivElement>) => {
    if (e.target === e.currentTarget) onClose();
  };

  const joinedDate = profile
    ? new Date(profile.createdAt).toLocaleDateString("en-US", {
        year: "numeric",
        month: "long",
        day: "numeric",
      })
    : "";

  const letter = profile?.displayName.charAt(0).toUpperCase() ?? "?";

  return (
    <div className="modal-overlay" onClick={handleBackdrop}>
      <div className="modal user-profile-modal">
        <div className="modal-header">
          <h2>Profile</h2>
          <button className="modal-close" onClick={onClose} title="Close">
            ✕
          </button>
        </div>

        {isLoading ? (
          <div className="centered">
            <div className="spinner" />
          </div>
        ) : profile ? (
          <div className="user-profile-body">
            <div className="user-profile-avatar-wrap">
              {profile.avatarUrl ? (
                <img
                  src={profile.avatarUrl}
                  alt={profile.displayName}
                  className="user-profile-avatar-img"
                />
              ) : (
                <div className="user-profile-avatar-letter">{letter}</div>
              )}
            </div>

            <p className="user-profile-name">{profile.displayName}</p>

            {profile.bio && (
              <p className="user-profile-bio">{profile.bio}</p>
            )}

            {profile.websiteUrl && (
              <a
                href={profile.websiteUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="user-profile-link"
              >
                🔗 {profile.websiteUrl}
              </a>
            )}

            <div className="user-profile-joined">Member since {joinedDate}</div>
          </div>
        ) : (
          <p className="user-profile-error">Profile not found.</p>
        )}
      </div>
    </div>
  );
}
