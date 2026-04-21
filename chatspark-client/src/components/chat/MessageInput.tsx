import { useState, useRef, useEffect } from "react";
import type { ChannelResponse } from "../../types/channel";

interface Props {
  channel: ChannelResponse;
  onSend: (content: string) => Promise<void>;
  onSendMedia: (file: File) => Promise<void>;
  onTyping: () => void;
  onStopTyping: () => void;
}

export function MessageInput({ channel, onSend, onSendMedia, onTyping, onStopTyping }: Props) {
  const [content, setContent] = useState("");
  const [isSending, setIsSending] = useState(false);
  const [isRecording, setIsRecording] = useState(false);
  const [recordingSeconds, setRecordingSeconds] = useState(0);

  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const imageInputRef = useRef<HTMLInputElement>(null);
  const mediaRecorderRef = useRef<MediaRecorder | null>(null);
  const audioChunksRef = useRef<Blob[]>([]);
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const streamRef = useRef<MediaStream | null>(null);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      if (timerRef.current) clearInterval(timerRef.current);
      streamRef.current?.getTracks().forEach((t) => t.stop());
    };
  }, []);

  const handleSubmit = async (e?: React.FormEvent) => {
    e?.preventDefault();
    const trimmed = content.trim();
    if (!trimmed || isSending) return;

    setIsSending(true);
    setContent("");
    onStopTyping();
    try {
      await onSend(trimmed);
    } finally {
      setIsSending(false);
      setTimeout(() => textareaRef.current?.focus(), 0);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      handleSubmit();
    }
  };

  const handleChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    setContent(e.target.value);
    if (e.target.value) onTyping();
    e.target.style.height = "auto";
    e.target.style.height = `${Math.min(e.target.scrollHeight, 200)}px`;
  };

  // --- Image upload ---
  const handleImageSelect = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    e.target.value = "";
    setIsSending(true);
    try {
      await onSendMedia(file);
    } finally {
      setIsSending(false);
      setTimeout(() => textareaRef.current?.focus(), 0);
    }
  };

  // --- Voice recording ---
  const startRecording = async () => {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      streamRef.current = stream;

      // Pick the best supported format
      const mimeType = MediaRecorder.isTypeSupported("audio/webm;codecs=opus")
        ? "audio/webm;codecs=opus"
        : MediaRecorder.isTypeSupported("audio/webm")
        ? "audio/webm"
        : "audio/ogg";

      const recorder = new MediaRecorder(stream, { mimeType });
      audioChunksRef.current = [];

      recorder.ondataavailable = (e) => {
        if (e.data.size > 0) audioChunksRef.current.push(e.data);
      };

      recorder.onstop = async () => {
        const blob = new Blob(audioChunksRef.current, { type: mimeType });
        const ext = mimeType.includes("ogg") ? "ogg" : "webm";
        const file = new File([blob], `voice-${Date.now()}.${ext}`, { type: mimeType });
        stream.getTracks().forEach((t) => t.stop());
        streamRef.current = null;
        setIsSending(true);
        try {
          await onSendMedia(file);
        } finally {
          setIsSending(false);
          setTimeout(() => textareaRef.current?.focus(), 0);
        }
      };

      mediaRecorderRef.current = recorder;
      recorder.start();
      setIsRecording(true);
      setRecordingSeconds(0);
      timerRef.current = setInterval(() => setRecordingSeconds((s) => s + 1), 1000);
    } catch {
      alert("Microphone access denied. Please allow microphone access to record voice messages.");
    }
  };

  const stopRecording = () => {
    if (timerRef.current) {
      clearInterval(timerRef.current);
      timerRef.current = null;
    }
    mediaRecorderRef.current?.stop();
    setIsRecording(false);
    setRecordingSeconds(0);
  };

  const cancelRecording = () => {
    if (timerRef.current) {
      clearInterval(timerRef.current);
      timerRef.current = null;
    }
    // Prevent onstop from uploading
    if (mediaRecorderRef.current) {
      mediaRecorderRef.current.onstop = null;
      mediaRecorderRef.current.stop();
    }
    streamRef.current?.getTracks().forEach((t) => t.stop());
    streamRef.current = null;
    audioChunksRef.current = [];
    setIsRecording(false);
    setRecordingSeconds(0);
  };

  const formatSeconds = (s: number) => {
    const m = Math.floor(s / 60);
    const sec = s % 60;
    return `${m}:${sec.toString().padStart(2, "0")}`;
  };

  return (
    <form className="message-input-form" onSubmit={handleSubmit}>
      {/* Hidden file input for images */}
      <input
        ref={imageInputRef}
        type="file"
        accept="image/jpeg,image/png,image/gif,image/webp"
        style={{ display: "none" }}
        onChange={handleImageSelect}
      />

      <div className="message-input-wrapper">
        {isRecording ? (
          /* Recording state */
          <div className="recording-indicator">
            <span className="recording-dot" />
            <span className="recording-time">{formatSeconds(recordingSeconds)}</span>
            <span className="recording-label">Recording…</span>
          </div>
        ) : (
          /* Normal text input */
          <textarea
            ref={textareaRef}
            className="message-input"
            value={content}
            onChange={handleChange}
            onKeyDown={handleKeyDown}
            placeholder={`Message #${channel.name}`}
            rows={1}
            disabled={isSending}
          />
        )}

        {/* Image picker button */}
        {!isRecording && (
          <button
            type="button"
            className="media-btn"
            title="Send image"
            disabled={isSending}
            onClick={() => imageInputRef.current?.click()}
          >
            📎
          </button>
        )}

        {/* Voice recorder button */}
        {isRecording ? (
          <>
            <button
              type="button"
              className="media-btn"
              title="Cancel recording"
              onClick={cancelRecording}
            >
              ✕
            </button>
            <button
              type="button"
              className="media-btn media-btn-send-voice"
              title="Send voice message"
              onClick={stopRecording}
            >
              ✓
            </button>
          </>
        ) : (
          <button
            type="button"
            className="media-btn"
            title="Record voice message"
            disabled={isSending}
            onClick={startRecording}
          >
            🎤
          </button>
        )}

        {/* Text send button — hidden while recording */}
        {!isRecording && (
          <button
            type="submit"
            className="message-send-btn"
            disabled={!content.trim() || isSending}
            title="Send message"
          >
            ↑
          </button>
        )}
      </div>

      {!isRecording && (
        <p className="message-input-hint">Press Enter to send · Shift+Enter for new line · 📎 image · 🎤 voice</p>
      )}
    </form>
  );
}
