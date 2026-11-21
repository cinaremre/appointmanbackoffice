import { Chip } from "@mui/material";

/**
 * Durum rozeti: renk tek basina gosterge degil; etiket metni
 * her zaman tasir (dokuman 5.2 erisilebilirlik).
 */
const COLOR_BY_STATUS: Record<string, "success" | "warning" | "error" | "info" | "default"> = {
  Active: "success",
  Aktif: "success",
  Suspended: "warning",
  Askida: "warning",
  Archived: "default",
  Cancelled: "error",
  Iptal: "error",
  Completed: "success",
  Tamamlandi: "success",
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
