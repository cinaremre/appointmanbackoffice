import { Alert, Box, Button, Card, CardContent, Stack, TextField, Typography } from "@mui/material";
import type { FormEvent } from "react";
import { useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../../../app/providers/AuthContext";

/** Giriş ekranı (doküman 7.1): hata mesajları açık, oturum sessionStorage'da. */
export function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as { from?: string } | null)?.from ?? "/";

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      await login(email, password);
      // İşletme sahibi kendi portalina, yönetim rolu dashboard'a yönlenir.
      const role = JSON.parse(sessionStorage.getItem("backoffice.auth") ?? "{}").role;
      const target = from !== "/" ? from : role === "BusinessOwner" ? "/owner" : "/";
      navigate(target, { replace: true });
    } catch (err) {
      setError(err instanceof Error ? err.message : "Giriş başarısız.");
    } finally {
      setBusy(false);
    }
  };

  return (
    <Box sx={{ minHeight: "100vh", display: "flex", alignItems: "center", justifyContent: "center", p: 2 }}>
      <Card sx={{ maxWidth: 400, width: "100%" }}>
        <CardContent sx={{ p: { xs: 2, sm: 4 } }}>
          <Typography variant="h5" gutterBottom>
            Rezervise Backoffice
          </Typography>
          <Typography color="text.secondary" sx={{ mb: 3 }}>
            Platform yönetim paneli girişi.
          </Typography>
          <form onSubmit={submit} aria-label="giriş formu">
            <Stack spacing={2}>
              <TextField
                label="E-posta"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                autoComplete="username"
              />
              <TextField
                label="Şifre"
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                autoComplete="current-password"
              />
              {error ? <Alert severity="error">{error}</Alert> : null}
              <Button type="submit" variant="contained" disabled={busy} size="large">
                {busy ? "Giriliyor..." : "Giriş Yap"}
              </Button>
            </Stack>
          </form>
        </CardContent>
      </Card>
    </Box>
  );
}
