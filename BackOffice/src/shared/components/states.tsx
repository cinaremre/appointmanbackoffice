import { Alert, Box, CircularProgress, Typography } from "@mui/material";
import type { ReactNode } from "react";
import { ApiError } from "../api/client";

/** Ortak durum bilesenleri (dokuman 30.4-6): loading/empty/error. */

export function LoadingState({ label = "Yukleniyor..." }: { label?: string }) {
  return (
    <Box sx={{ display: "flex", alignItems: "center", gap: 2, py: 6, justifyContent: "center" }}>
      <CircularProgress size={24} aria-label="yukleniyor" />
      <Typography color="text.secondary">{label}</Typography>
    </Box>
  );
}

export function EmptyState({ label = "Kayit bulunamadi." }: { label?: string }) {
  return (
    <Box sx={{ py: 6, textAlign: "center" }}>
      <Typography color="text.secondary">{label}</Typography>
    </Box>
  );
}

export function ErrorState({ error, hint }: { error: unknown; hint?: string }) {
  const message =
    error instanceof ApiError ? error.userMessage : "Veri alinamadi. Lutfen tekrar deneyin.";
  return (
    <Box sx={{ py: 4, px: 2 }}>
      <Alert severity="error">
        {message}
        {error instanceof ApiError && error.traceId ? (
          <Typography variant="caption" sx={{ mt: 0.5, display: "block" }}>
            Destek icin traceId: {error.traceId}
          </Typography>
        ) : null}
        {hint ? (
          <Typography variant="caption" sx={{ mt: 0.5, display: "block" }}>
            {hint}
          </Typography>
        ) : null}
      </Alert>
    </Box>
  );
}

export function PageHeader({ title, actions }: { title: string; actions?: ReactNode }) {
  return (
    <Box
      sx={{
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        gap: 1,
        flexWrap: "wrap",
        mb: 2,
      }}
    >
      <Typography variant="h5" component="h1">
        {title}
      </Typography>
      {actions}
    </Box>
  );
}
