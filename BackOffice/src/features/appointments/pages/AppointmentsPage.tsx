import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogContent,
  DialogTitle,
  MenuItem,
  Snackbar,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { useSearchParams } from "react-router-dom";
import { useAuth } from "../../../app/providers/AuthContext";
import { ApiError, getItem, getList, postItem } from "../../../shared/api/client";
import { toQueryString } from "../../../shared/api/types";
import { DataTable } from "../../../shared/components/DataTable";
import type { DataTableColumn } from "../../../shared/components/DataTable";
import { ReasonConfirmDialog } from "../../../shared/components/ReasonConfirmDialog";
import { StatusBadge } from "../../../shared/components/StatusBadge";
import { EmptyState, ErrorState, LoadingState, PageHeader } from "../../../shared/components/states";
import { PERMISSIONS } from "../../../shared/constants/permissions";
import { APPOINTMENT_STATUSES, appointmentStatusLabel } from "../types";
import type { AppointmentConflict, AppointmentDetail, AppointmentListItem, AppointmentStatus } from "../types";

/**
 * Randevular (dokuman 13): filtreler URL query ile senkron. Iptal
 * aksiyonunda domain kurallari SUNUCUDA uygulanir (dokuman 13.3);
 * frontend durum kurallari belirlemez, 409/400 hatalari acik gosterilir.
 */

const CANCEL_IMPACT =
  "Randevu iptali sunucudaki domain kurallarina gore uygulanir; uygun olmayan durumlar sunucu tarafindan 409/400 ile reddedilir ve hata burada gosterilir. Iptal geri alinamaz. Gerekceniz denetim kaydina yazilir.";

function formatDate(value?: string | null): string {
  if (!value) return "-";
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "-" : date.toLocaleString("tr-TR");
}

/** ApiError'dan kullaniciya uygun mesaj + traceId uretir. */
function errorText(error: unknown): string {
  if (error instanceof ApiError) {
    return error.traceId ? `${error.userMessage} (traceId: ${error.traceId})` : error.userMessage;
  }
  return "Islem basarisiz. Lutfen tekrar deneyin.";
}

/** Detay diyalogu: GET /appointments/{id} — detay + history. */
function AppointmentDetailDialog({ appointmentId, onClose }: { appointmentId: string | null; onClose: () => void }) {
  const detailQuery = useQuery({
    queryKey: ["appointments", "detail", appointmentId],
    queryFn: (ctx) => getItem<AppointmentDetail>(`/appointments/${appointmentId!}`, ctx.signal),
    enabled: appointmentId !== null,
  });

  const detail = detailQuery.data;

  return (
    <Dialog open={appointmentId !== null} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Randevu Detayi</DialogTitle>
      <DialogContent>
        {detailQuery.isLoading ? (
          <LoadingState />
        ) : detailQuery.isError ? (
          <ErrorState error={detailQuery.error} />
        ) : detail ? (
          <Stack spacing={2} sx={{ mt: 1 }}>
            <Stack spacing={0.5}>
              <Typography variant="subtitle2">Randevu</Typography>
              <Typography>Randevu No: {detail.id}</Typography>
              <Typography>Isletme: {detail.businessId}</Typography>
              <Typography>Kullanici: {detail.userId}</Typography>
              <Typography>Calisan: {detail.employeeId}</Typography>
              <Typography>Hizmet: {detail.serviceId}</Typography>
              <Typography>Tarih: {formatDate(detail.appointmentDate)}</Typography>
              <Typography>
                Durum: <StatusBadge status={appointmentStatusLabel(detail.status)} />
              </Typography>
              <Typography>Notlar: {detail.notes ?? "-"}</Typography>
            </Stack>
            <Box>
              <Typography variant="subtitle2" gutterBottom>
                Gecmis
              </Typography>
              {detail.history.length === 0 ? (
                <Typography color="text.secondary">Kayit yok.</Typography>
              ) : (
                detail.history.map((entry, index) => (
                  <Typography key={`${entry.action}-${entry.changedAt}-${index}`}>
                    {formatDate(entry.changedAt)} - {entry.action}
                    {entry.note ? ` - ${entry.note}` : ""}
                    {entry.actorId ? ` (aktor: ${entry.actorId})` : ""}
                  </Typography>
                ))
              )}
            </Box>
          </Stack>
        ) : null}
      </DialogContent>
    </Dialog>
  );
}

export function AppointmentsPage() {
  // Filtreler URL query'de yasir: paylasilabilir baglantilar ve geri
  // tusu davranisi dogru olur.
  const [searchParams, setSearchParams] = useSearchParams();
  const { has_permission } = useAuth();
  const queryClient = useQueryClient();
  const canCancel = has_permission(PERMISSIONS.appointmentCancel);

  const rawStatus = searchParams.get("status") ?? "";
  // URL'e elle yazilan gecersiz durumlarini secimde gosterme.
  const status: string = (APPOINTMENT_STATUSES as readonly string[]).includes(rawStatus) ? rawStatus : "";
  const fromDate = searchParams.get("fromDate") ?? "";
  const toDate = searchParams.get("toDate") ?? "";
  const businessId = searchParams.get("businessId") ?? "";
  const page = Math.max(1, Number(searchParams.get("page")) || 1);
  const pageSize = Number(searchParams.get("pageSize")) || 50;

  const setParam = (key: string, value: string) => {
    setSearchParams(
      (prev) => {
        const next = new URLSearchParams(prev);
        if (value === "") next.delete(key);
        else next.set(key, value);
        // Filtre degisince sayfa bastan baslar.
        if (key !== "page" && key !== "pageSize") next.delete("page");
        return next;
      },
      { replace: true },
    );
  };

  const clearFilters = () => {
    setSearchParams(new URLSearchParams(), { replace: true });
  };

  const listQuery = useQuery({
    queryKey: ["appointments", "list", { status, fromDate, toDate, businessId, page, pageSize }],
    queryFn: (ctx) =>
      getList<AppointmentListItem>(
        `/appointments${toQueryString({
          businessId: businessId.trim() || undefined,
          status: (status || undefined) as AppointmentStatus | undefined,
          fromDate: fromDate || undefined,
          toDate: toDate || undefined,
          page,
          pageSize,
        })}`,
        ctx.signal,
      ),
  });

  // Iptal: reason ZORUNLU; domain kurallari sunucuda uygulanir.
  const cancelMutation = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      postItem(`/appointments/${id}/cancel`, { reason }),
    onSuccess: () => {
      setPending(null);
      setSnackbar({ severity: "success", message: "Randevu iptal istegi uygulandi." });
      void queryClient.invalidateQueries({ queryKey: ["appointments"] });
    },
    onError: (error: unknown) => {
      // Dialog acik kalir; kullanici gerekceyi duzeltip tekrar deneyebilir.
      setSnackbar({ severity: "error", message: errorText(error) });
    },
  });

  const [pending, setPending] = useState<{ id: string } | null>(null);
  const [detailId, setDetailId] = useState<string | null>(null);
  const [snackbar, setSnackbar] = useState<{ severity: "success" | "error"; message: string } | null>(null);

  // Cakisma sorgusu: businessId + date ile sorgulanir, kucuk liste.
  const [conflictBusinessId, setConflictBusinessId] = useState("");
  const [conflictDate, setConflictDate] = useState("");
  const [conflictQuery, setConflictQuery] = useState<{ businessId: string; date: string } | null>(null);

  const conflictsQuery = useQuery({
    queryKey: ["appointments", "conflicts", conflictQuery],
    queryFn: (ctx) =>
      getList<AppointmentConflict>(
        `/appointments/conflicts${toQueryString({
          businessId: conflictQuery!.businessId,
          date: conflictQuery!.date,
        })}`,
        ctx.signal,
      ),
    enabled: conflictQuery !== null,
  });

  const columns: DataTableColumn<AppointmentListItem>[] = [
    { key: "date", header: "Tarih", render: (row) => formatDate(row.appointmentDate) },
    { key: "business", header: "Isletme", render: (row) => row.businessId },
    { key: "employee", header: "Calisan", render: (row) => row.employeeId },
    { key: "service", header: "Hizmet", render: (row) => row.serviceId },
    { key: "status", header: "Durum", render: (row) => <StatusBadge status={appointmentStatusLabel(row.status)} /> },
    { key: "notes", header: "Notlar", render: (row) => row.notes ?? "-" },
  ];

  return (
    <Box>
      <PageHeader title="Randevular" />

      <Stack
        direction={{ xs: "column", md: "row" }}
        spacing={2}
        sx={{ mb: 2, alignItems: { xs: "stretch", md: "center" } }}
      >
        <TextField
          select
          size="small"
          label="Durum"
          value={status}
          onChange={(e) => setParam("status", e.target.value)}
          sx={{ minWidth: 160 }}
        >
          <MenuItem value="">Tumu</MenuItem>
          {APPOINTMENT_STATUSES.map((value) => (
            <MenuItem key={value} value={value}>
              {value}
            </MenuItem>
          ))}
        </TextField>
        <TextField
          size="small"
          type="date"
          label="Baslangic Tarihi"
          value={fromDate}
          onChange={(e) => setParam("fromDate", e.target.value)}
          slotProps={{ inputLabel: { shrink: true } }}
          sx={{ minWidth: 180 }}
        />
        <TextField
          size="small"
          type="date"
          label="Bitis Tarihi"
          value={toDate}
          onChange={(e) => setParam("toDate", e.target.value)}
          slotProps={{ inputLabel: { shrink: true } }}
          sx={{ minWidth: 180 }}
        />
        <TextField
          size="small"
          label="Isletme ID"
          value={businessId}
          onChange={(e) => setParam("businessId", e.target.value)}
          sx={{ minWidth: 220 }}
        />
        <Button variant="outlined" color="inherit" onClick={clearFilters}>
          Temizle
        </Button>
      </Stack>

      <DataTable
        columns={columns}
        rows={listQuery.data?.items}
        loading={listQuery.isLoading}
        error={listQuery.error}
        page={page}
        pageSize={pageSize}
        totalCount={listQuery.data?.pagination.totalCount ?? 0}
        onPageChange={(nextPage) => setParam("page", String(nextPage))}
        onPageSizeChange={(nextSize) => setParam("pageSize", String(nextSize))}
        emptyLabel="Randevu bulunamadi."
        ariaLabel="randevular tablosu"
        onRefresh={() => void listQuery.refetch()}
        rowActions={(row) => (
          <Stack direction="row" spacing={1} sx={{ justifyContent: "flex-end" }}>
            <Button size="small" onClick={() => setDetailId(row.id)}>
              Detay
            </Button>
            {canCancel ? (
              <Button size="small" color="error" onClick={() => setPending({ id: row.id })}>
                Iptal
              </Button>
            ) : null}
          </Stack>
        )}
      />

      <AppointmentDetailDialog appointmentId={detailId} onClose={() => setDetailId(null)} />

      <ReasonConfirmDialog
        open={pending !== null}
        title={pending ? `Randevu Iptali: ${pending.id}` : ""}
        impact={CANCEL_IMPACT}
        confirmLabel="Iptal Et"
        busy={cancelMutation.isPending}
        onCancel={() => setPending(null)}
        onConfirm={(reason) => {
          if (pending) cancelMutation.mutate({ id: pending.id, reason });
        }}
      />

      {snackbar ? (
        <Snackbar open autoHideDuration={6000} onClose={() => setSnackbar(null)}>
          <Alert severity={snackbar.severity} onClose={() => setSnackbar(null)}>
            {snackbar.message}
          </Alert>
        </Snackbar>
      ) : null}

      <Box sx={{ mt: 4 }}>
        <Stack
          direction={{ xs: "column", md: "row" }}
          spacing={2}
          sx={{ alignItems: { xs: "stretch", md: "center" } }}
        >
          <Typography variant="h6" component="h2">
            Cakisma Sorgusu
          </Typography>
          <TextField
            size="small"
            label="Isletme ID"
            value={conflictBusinessId}
            onChange={(e) => setConflictBusinessId(e.target.value)}
            sx={{ minWidth: 220 }}
          />
          <TextField
            size="small"
            type="date"
            label="Tarih"
            value={conflictDate}
            onChange={(e) => setConflictDate(e.target.value)}
            slotProps={{ inputLabel: { shrink: true } }}
            sx={{ minWidth: 180 }}
          />
          <Button
            variant="outlined"
            disabled={!conflictBusinessId.trim() || !conflictDate}
            onClick={() =>
              setConflictQuery({ businessId: conflictBusinessId.trim(), date: conflictDate })
            }
          >
            Sorgula
          </Button>
        </Stack>

        {conflictQuery ? (
          <Box sx={{ mt: 2 }}>
            {conflictsQuery.isLoading ? (
              <LoadingState label="Cakismalar sorgulaniyor..." />
            ) : conflictsQuery.isError ? (
              <ErrorState error={conflictsQuery.error} />
            ) : conflictsQuery.data && conflictsQuery.data.items.length > 0 ? (
              <Table size="small" aria-label="cakisan randevular tablosu">
                <TableHead>
                  <TableRow>
                    <TableCell sx={{ fontWeight: 600 }}>Randevu</TableCell>
                    <TableCell sx={{ fontWeight: 600 }}>Calisan</TableCell>
                    <TableCell sx={{ fontWeight: 600 }}>Tarih</TableCell>
                    <TableCell sx={{ fontWeight: 600 }}>Isletme</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {conflictsQuery.data.items.map((conflict) => (
                    <TableRow key={conflict.appointmentId} hover>
                      <TableCell>{conflict.appointmentId}</TableCell>
                      <TableCell>{conflict.employeeId}</TableCell>
                      <TableCell>{formatDate(conflict.appointmentDate)}</TableCell>
                      <TableCell>{conflict.businessId}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            ) : (
              <EmptyState label="Cakisan randevu bulunamadi." />
            )}
          </Box>
        ) : null}
      </Box>
    </Box>
  );
}
