import { Box, Typography } from "@mui/material";
import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useAuth } from "../providers/AuthContext";
import type { OwnerPermission, Permission } from "../../shared/constants/permissions";

/**
 * Route guard'ları (doküman 7.2): oturum + permission kontrolü.
 * Yetkisiz erişimde 403 ekranı; oturumsuzda login'e yönlendirme.
 */

export function RequireAuth({ children }: { children: ReactNode }) {
  const { is_authenticated } = useAuth();
  const location = useLocation();

  if (!is_authenticated) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  }

  return <>{children}</>;
}

export function RequirePermission({ permission, children }: { permission: Permission | OwnerPermission; children: ReactNode }) {
  const { has_permission } = useAuth();

  if (!has_permission(permission)) {
    return (
      <Box sx={{ py: 8, textAlign: "center" }}>
        <Typography variant="h6" gutterBottom>
          Bu sayfaya erişim yetkiniz yok.
        </Typography>
        <Typography color="text.secondary">
          Gerekli izin: {permission}. Nihai yetki kontrolü sunucuda yapılır.
        </Typography>
      </Box>
    );
  }

  return <>{children}</>;
}
