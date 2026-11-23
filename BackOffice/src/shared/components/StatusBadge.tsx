import { Chip } from "@mui/material";

/**
 * Durum rozeti: renk tek başına gösterge değil; etiket metni
 * her zaman taşır (doküman 5.2 erişilebilirlik).
 */
const COLOR_BY_STATUS: Record<string, "success" | "warning" | "error" | "info" | "default"> = {
  Active: "success",
  Aktif: "success",
  Suspended: "warning",
  Askıda: "warning",
  Archived: "default",
  Cancelled: "error",
  İptal: "error",
  Completed: "success",
  Tamamlandı: "success",
  Pending: "info",
  Beklemede: "info",
  Confirmed: "info",
  NoShow: "error",
  Failed: "error",
  Locked: "error",
  Healthy: "success",
  Running: "info",
};

export function StatusBadge({ status }: { status: string }) {
  const color = COLOR_BY_STATUS[status] ?? "default";
  return <Chip size="small" color={color} label={status} variant="outlined" />;
}
