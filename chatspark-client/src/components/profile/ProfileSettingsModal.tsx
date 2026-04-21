import { useState, useRef, useEffect } from "react";
import { useAuth } from "../../context/AuthContext";
import { profileApi } from "../../api/profileApi";

interface Props {
  onClose: () => void;
}

export function ProfileSettingsModal({ onClose }: Props) {
  const { user, updateProfile } = useAuth();

  const [displayName, setDisplayName] = useState(user?.displayName ?? "");
  const [bio, setBio] = useState(user?.bio ?? "");
  const [websiteUrl, setWebsiteUrl] = useState(user?.websiteUrl ?? "");

  // File upload state
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(user?.avatarUrl ?? null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState("");
  const [saved, setSaved] = useState(false);

  // Clean up the object URL when component unmounts or preview changes
  useEffect(() => {
    return () => {
      if (previewUrl && previewUrl.startsWith("blob:")) {
        URL.revokeObjectURL(previewUrl);
      }
    };
  }, [previewUrl]);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    if (!["image/jpeg", "image/png"].includes(file.type)) {
      setError("Only JPEG and PNG files are allowed.");
      return;
    }
    if (file.size > 2 * 1024 * 1024) {
      setError("File must be under 2MB.");
      return;
    }

    setError("");
    setSelectedFile(file);

    // Revoke previous blob URL if any
    if (previewUrl?.startsWith("blob:")) URL.revokeObjectURL(previewUrl);
    setPreviewUrl(URL.createObjectURL(file));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setIsSaving(true);

    try {
      let avatarUrl: string | undefined;

      // If user picked a new file, upload it first
      if (selectedFile) {
        avatarUrl = await profileApi.uploadAvatar(selectedFile);
      }

      await updateProfile({
        displayName: displayName.trim() || undefined,
        avatarUrl,
        bio: bio.trim() || undefined,
        websiteUrl: websiteUrl.trim() || undefined,
      });

      setSelectedFile(null);
      setSaved(true);
      setTimeout(() => setSaved(false), 2000);
    } catch {
      setError("Failed to save profile. Please try again.");
    } finally {
      setIsSaving(false);
    }
  };

  const avatarLetter = (user?.displayName ?? "?").charAt(0).toUpperCase();

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal profile-modal" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Profile Settings</h2>
          <button className="modal-close" onClick={onClose}>✕</button>
        </div>

        {/* Avatar preview + click to change */}
        <div className="profile-avatar-preview">
          <div
            className="profile-avatar-clickable"
            onClick={() => fileInputRef.current?.click()}
            title="Click to change avatar"
          >
            {previewUrl ? (
              <img src={previewUrl} alt="avatar" className="profile-avatar-img" />
            ) : (
              <div className="profile-avatar-placeholder">{avatarLetter}</div>
            )}
            <div className="profile-avatar-overlay">📷</div>
          </div>
          <p className="form-hint">Click to upload (JPG or PNG, max 2MB)</p>

          {/* Hidden file input — triggered by clicking the avatar */}
          <input
            ref={fileInputRef}
            type="file"
            accept="image/jpeg,image/png"
            style={{ display: "none" }}
            onChange={handleFileChange}
          />
        </div>

        <form onSubmit={handleSubmit} className="modal-form">
          {error && <div className="auth-error">{error}</div>}

          <div className="form-group">
            <label>Display name</label>
            <input
              type="text"
              value={displayName}
              onChange={(e) => setDisplayName(e.target.value)}
              placeholder="Your name"
              maxLength={100}
            />
          </div>

          <div className="form-group">
            <label>Bio</label>
            <textarea
              value={bio}
              onChange={(e) => setBio(e.target.value)}
              placeholder="Tell people a little about yourself"
              maxLength={300}
              rows={3}
            />
            <span className="form-hint">{bio.length}/300</span>
          </div>

          <div className="form-group">
            <label>Website</label>
            <input
              type="url"
              value={websiteUrl}
              onChange={(e) => setWebsiteUrl(e.target.value)}
              placeholder="https://yoursite.com"
            />
          </div>

          <div className="modal-actions">
            <button type="button" className="btn-secondary" onClick={onClose}>
              Cancel
            </button>
            <button type="submit" className="btn-primary" disabled={isSaving}>
              {isSaving ? "Saving..." : saved ? "Saved!" : "Save changes"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
