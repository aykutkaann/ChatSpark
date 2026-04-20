import { LoginForm } from "../components/auth/LoginForm";
import "../styles/auth.css";

export function LoginPage() {
  return (
    <div className="auth-page">
      <div className="auth-brand">
        <h1 className="brand-logo">ChatSpark</h1>
        <p className="brand-tagline">Real-time collaboration, reimagined.</p>
      </div>
      <LoginForm />
    </div>
  );
}
