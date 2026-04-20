import { RegisterForm } from "../components/auth/RegisterForm";
import "../styles/auth.css";

export function RegisterPage() {
  return (
    <div className="auth-page">
      <div className="auth-brand">
        <h1 className="brand-logo">ChatSpark</h1>
        <p className="brand-tagline">Real-time collaboration, reimagined.</p>
      </div>
      <RegisterForm />
    </div>
  );
}
