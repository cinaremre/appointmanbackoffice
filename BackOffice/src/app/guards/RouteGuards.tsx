import { Box, Typography } from "@mui/material";
import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useAuth } from "../providers/AuthContext";
import type { OwnerPermission, Permission } from "../../shared/constants/permissions";

/**
 * Route guard'lari (dokuman 7.2): oturum + permission kontrolu.
 * Yetkisiz erisimde 403 ekrani; oturumsuzda login'e yonlendirme.
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
          Bu sayfaya erisim yetkiniz yok.
        </Typography>
        <Typography color="text.secondary">
          Gerekli izin: {permission}. Nihai yetki kontrolu sunucuda yapilir.
        </Typography>
      </Box>
    );
  }

  return <>{children}</>;
}
