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
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
  useMediaQuery,
  useTheme,
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
 * Randevular (doküman 13): filtreler URL query ile senkron. İptal
 * aksiyonunda domain kuralları SUNUCUDA uygulanır (doküman 13.3);
 * frontend durum kuralları belirlemez, 409/400 hataları açık gösterilir.
 */

const CANCEL_IMPACT =
  "Randevu iptali sunucudaki domain kurallarına göre uygulanır; uygun olmayan durumlar sunucu tarafından 409/400 ile reddedilir ve hata burada gösterilir. İptal geri alınamaz. Gerekçeniz denetim kaydına yazılır.";

function formatDate(value?: string | null): string {
  if (!value) return "-";
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "-" : date.toLocaleString("tr-TR");
}

/** ApiError'dan kullanıcıya uygun mesaj + traceId üretir. */
function errorText(error: unknown): string {
  if (error instanceof ApiError) {
    return error.traceId ? `${error.userMessage} (traceId: ${error.traceId})` : error.userMessage;
  }
  return "İşlem başarısız. Lütfen tekrar deneyin.";
}

/** Uzun GUID'leri tablo içinde tek satırda kısaltarak gösterir. */
function IdText({ value }: { value: string }) {
  return (
    <Typography variant="body2" noWrap title={value} sx={{ maxWidth: 180 }}>
      {value}
    </Typography>
  );
}

/** Detay diyalogu: GET /appointments/{id} — detay + history. */
function AppointmentDetailDialog({ appointmentId, onClose }: { appointmentId: string | null; onClose: () => void }) {
  const theme = useTheme();
  const is_mobile = useMediaQuery(theme.breakpoints.down("sm"));
  const detailQuery = useQuery({
    queryKey: ["appointments", "detail", appointmentId],
    queryFn: (ctx) => getItem<AppointmentDetail>(`/appointments/${appointmentId!}`, ctx.signal),
    enabled: appointmentId !== null,
  });

  const detail = detailQuery.data;

  return (
    <Dialog open={appointmentId !== null} onClose={onClose} maxWidth="sm" fullWidth fullScreen={is_mobile}>
      <DialogTitle>Randevu Detayı</DialogTitle>
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
              <Typography>İşletme: {detail.businessId}</Typography>
              <Typography>Kullanıcı: {detail.userId}</Typography>
              <Typography>Çalışan: {detail.employeeId}</Typography>
              <Typography>Hizmet: {detail.serviceId}</Typography>
              <Typography>Tarih: {formatDate(detail.appointmentDate)}</Typography>
              <Typography>
                Durum: <StatusBadge status={appointmentStatusLabel(detail.status)} />
              </Typography>
              <Typography>Notlar: {detail.notes ?? "-"}</Typography>
            </Stack>
            <Box>
              <Typography variant="subtitle2" gutterBottom>
                Geçmiş
              </Typography>
              {detail.history.length === 0 ? (
                <Typography color="text.secondary">Kayıt yok.</Typography>
              ) : (
                detail.history.map((entry, index) => (
                  <Typography key={`${entry.action}-${entry.changedAt}-${index}`}>
                    {formatDate(entry.changedAt)} - {entry.action}
                    {entry.note ? ` - ${entry.note}` : ""}
                    {entry.actorId ? ` (aktör: ${entry.actorId})` : ""}
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
  // Filtreler URL query'de yasir: paylaşılabilir bağlantılar ve geri
  // tuşu davranışı doğru olur.
  const [searchParams, setSearchParams] = useSearchParams();
  const { has_permission } = useAuth();
  const queryClient = useQueryClient();
  const canCancel = has_permission(PERMISSIONS.appointmentCancel);

  const rawStatus = searchParams.get("status") ?? "";
  // URL'e elle yazılan geçersiz durumlarını seçimde gösterme.
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
        // Filtre değişince sayfa bastan başlar.
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

  // İptal: reason ZORUNLU; domain kuralları sunucuda uygulanır.
  const cancelMutation = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      postItem(`/appointments/${id}/cancel`, { reason }),
    onSuccess: () => {
      setPending(null);
      setSnackbar({ severity: "success", message: "Randevu iptal isteği uygulandı." });
      void queryClient.invalidateQueries({ queryKey: ["appointments"] });
    },
    onError: (error: unknown) => {
      // Dialog açık kalır; kullanıcı gerekçeyi düzeltip tekrar deneyebilir.
      setSnackbar({ severity: "error", message: errorText(error) });
    },
  });

  const [pending, setPending] = useState<{ id: string } | null>(null);
  const [detailId, setDetailId] = useState<string | null>(null);
  const [snackbar, setSnackbar] = useState<{ severity: "success" | "error"; message: string } | null>(null);

  // Çakışma sorgusu: businessId + date ile sorgulanır, küçük liste.
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
    { key: "business", header: "İşletme", render: (row) => <IdText value={row.businessId} /> },
    { key: "employee", header: "Çalışan", render: (row) => <IdText value={row.employeeId} /> },
    { key: "service", header: "Hizmet", render: (row) => <IdText value={row.serviceId} /> },
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
          sx={{ minWidth: { xs: 0, sm: 160 } }}
        >
          <MenuItem value="">Tümü</MenuItem>
          {APPOINTMENT_STATUSES.map((value) => (
            <MenuItem key={value} value={value}>
              {value}
            </MenuItem>
          ))}
        </TextField>
        <TextField
          size="small"
          type="date"
          label="Başlangıç Tarihi"
          value={fromDate}
          onChange={(e) => setParam("fromDate", e.target.value)}
          slotProps={{ inputLabel: { shrink: true } }}
          sx={{ minWidth: { xs: 0, sm: 180 } }}
        />
        <TextField
          size="small"
          type="date"
          label="Bitiş Tarihi"
          value={toDate}
          onChange={(e) => setParam("toDate", e.target.value)}
          slotProps={{ inputLabel: { shrink: true } }}
          sx={{ minWidth: { xs: 0, sm: 180 } }}
        />
        <TextField
          size="small"
          label="İşletme ID"
          value={businessId}
          onChange={(e) => setParam("businessId", e.target.value)}
          sx={{ minWidth: { xs: 0, sm: 220 } }}
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
        emptyLabel="Randevu bulunamadı."
        ariaLabel="randevular tablosu"
        onRefresh={() => void listQuery.refetch()}
        rowActions={(row) => (
          <Stack direction="row" spacing={1} sx={{ justifyContent: "flex-end" }}>
            <Button size="small" onClick={() => setDetailId(row.id)}>
              Detay
            </Button>
            {canCancel ? (
              <Button size="small" color="error" onClick={() => setPending({ id: row.id })}>
                İptal
              </Button>
            ) : null}
          </Stack>
        )}
      />

      <AppointmentDetailDialog appointmentId={detailId} onClose={() => setDetailId(null)} />

      <ReasonConfirmDialog
        open={pending !== null}
        title={pending ? `Randevu İptali: ${pending.id}` : ""}
        impact={CANCEL_IMPACT}
        confirmLabel="İptal Et"
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
            Çakışma Sorgusu
          </Typography>
          <TextField
            size="small"
            label="İşletme ID"
            value={conflictBusinessId}
            onChange={(e) => setConflictBusinessId(e.target.value)}
            sx={{ minWidth: { xs: 0, sm: 220 } }}
          />
          <TextField
            size="small"
            type="date"
            label="Tarih"
            value={conflictDate}
            onChange={(e) => setConflictDate(e.target.value)}
            slotProps={{ inputLabel: { shrink: true } }}
            sx={{ minWidth: { xs: 0, sm: 180 } }}
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
              <LoadingState label="Çakışmalar sorgulanıyor..." />
            ) : conflictsQuery.isError ? (
              <ErrorState error={conflictsQuery.error} />
            ) : conflictsQuery.data && conflictsQuery.data.items.length > 0 ? (
              <TableContainer sx={{ maxHeight: 360 }}>
                <Table size="small" aria-label="çakışan randevular tablosu">
                  <TableHead>
                    <TableRow>
                      <TableCell sx={{ fontWeight: 600 }}>Randevu</TableCell>
                      <TableCell sx={{ fontWeight: 600 }}>Çalışan</TableCell>
                      <TableCell sx={{ fontWeight: 600 }}>Tarih</TableCell>
                      <TableCell sx={{ fontWeight: 600 }}>İşletme</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {conflictsQuery.data.items.map((conflict) => (
                      <TableRow key={conflict.appointmentId} hover>
                        <TableCell>
                          <IdText value={conflict.appointmentId} />
                        </TableCell>
                        <TableCell>
                          <IdText value={conflict.employeeId} />
                        </TableCell>
                        <TableCell>{formatDate(conflict.appointmentDate)}</TableCell>
                        <TableCell>
                          <IdText value={conflict.businessId} />
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            ) : (
              <EmptyState label="Çakışan randevu bulunamadı." />
            )}
          </Box>
        ) : null}
      </Box>
    </Box>
  );
}
