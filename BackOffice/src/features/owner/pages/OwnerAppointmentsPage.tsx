import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useSearchParams } from "react-router-dom";
import { Alert, Box, Button, MenuItem, Snackbar, Stack, TextField, Typography } from "@mui/material";
import { ApiError } from "../../../shared/api/client";
import { toQueryString } from "../../../shared/api/types";
import { DataTable } from "../../../shared/components/DataTable";
import type { DataTableColumn } from "../../../shared/components/DataTable";
import { ReasonConfirmDialog } from "../../../shared/components/ReasonConfirmDialog";
import { StatusBadge } from "../../../shared/components/StatusBadge";
import { EmptyState, ErrorState, LoadingState, PageHeader } from "../../../shared/components/states";
import { useAuth } from "../../../app/providers/AuthContext";
import { OWNER_PERMISSIONS } from "../../../shared/constants/permissions";
import { ownerGetList, ownerPostItem, useOwnedBusinesses } from "../api/ownerClient";
import type { OwnerAppointmentListItem } from "../types";
import { APPOINTMENT_STATUS_OPTIONS, appointmentStatusLabel, formatDateTime, shortId } from "../types";

/**
 * Owner randevuları: işletme kapsamında liste + iptal. Filtreler URL query
 * ile senkron (paylaşılabilir bağlantı, geri tuşu doğru davranış). İptal
 * gerekçesi ZORUNLU; domain kuralları SUNUCUDA uygulanır (doküman 13.3).
 */

const CANCEL_IMPACT =
  "Randevu iptali sunucudaki domain kurallarına göre uygulanır; uygun olmayan durumlar sunucu tarafından 409/400 ile reddedilir ve hata burada gösterilir. İptal geri alınamaz. Gerekçeniz denetim kaydına yazılır.";

function errorText(error: unknown): string {
  if (error instanceof ApiError) {
    return error.traceId ? `${error.userMessage} (traceId: ${error.traceId})` : error.userMessage;
  }
  return "İşlem başarısız. Lütfen tekrar deneyin.";
}

function GuidText({ value }: { value: string }) {
  return (
    <Typography variant="body2" component="span" title={value}>
      {shortId(value)}
    </Typography>
  );
}

export function OwnerAppointmentsPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const queryClient = useQueryClient();
  const { has_permission } = useAuth();
  const canCancel = has_permission(OWNER_PERMISSIONS.appointments);

  const businessesQuery = useOwnedBusinesses();
  const businesses = businessesQuery.data?.items ?? [];
  const [selectedId, setSelectedId] = useState("");

  // Tek işletme otomatik seçilir; açık seçim yoksa ilk işletme kullanılır.
  const businessId = selectedId || businesses[0]?.id || "";

  const rawStatus = searchParams.get("status") ?? "";
  const status = APPOINTMENT_STATUS_OPTIONS.some((option) => option.value === rawStatus)
    ? rawStatus
    : "";
  const fromDate = searchParams.get("fromDate") ?? "";
  const toDate = searchParams.get("toDate") ?? "";
  const page = Math.max(1, Number(searchParams.get("page")) || 1);
  const pageSize = Math.min(200, Math.max(25, Number(searchParams.get("pageSize")) || 50));

  const [pending, setPending] = useState<{ id: string } | null>(null);
  const [snackbar, setSnackbar] = useState<{ severity: "success" | "error"; message: string } | null>(
    null,
  );

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
    queryKey: ["owner", "appointments", businessId, { status, fromDate, toDate, page, pageSize }],
    queryFn: ({ signal }) =>
      ownerGetList<OwnerAppointmentListItem>(
        `/businesses/${businessId}/appointments${toQueryString({
          status: status === "" ? undefined : Number(status),
          fromDate: fromDate || undefined,
          toDate: toDate || undefined,
          page,
          pageSize,
        })}`,
        signal,
      ),
    enabled: businessId !== "",
  });

  // İptal: reason ZORUNLU; domain kuralları sunucuda uygulanır.
  const cancelMutation = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      ownerPostItem(`/businesses/${businessId}/appointments/${id}/cancel`, { reason }),
    onSuccess: () => {
      setPending(null);
      setSnackbar({ severity: "success", message: "Randevu iptal isteği uygulandı." });
      void queryClient.invalidateQueries({ queryKey: ["owner", "appointments"] });
    },
    onError: (error: unknown) => {
      // Dialog açık kalır; kullanıcı gerekçeyi düzeltip tekrar deneyebilir.
      setSnackbar({ severity: "error", message: errorText(error) });
    },
  });

  const columns: DataTableColumn<OwnerAppointmentListItem>[] = [
    { key: "date", header: "Tarih", render: (row) => formatDateTime(row.appointmentDate) },
    { key: "user", header: "Müşteri", render: (row) => <GuidText value={row.userId} /> },
    { key: "employee", header: "Çalışan", render: (row) => <GuidText value={row.employeeId} /> },
    { key: "service", header: "Hizmet", render: (row) => <GuidText value={row.serviceId} /> },
    {
      key: "status",
      header: "Durum",
      render: (row) => <StatusBadge status={appointmentStatusLabel(row.status)} />,
    },
    { key: "notes", header: "Notlar", render: (row) => row.notes ?? "-" },
  ];

  if (businessesQuery.isLoading) return <LoadingState />;
  if (businessesQuery.isError) return <ErrorState error={businessesQuery.error} />;
  if (businesses.length === 0) {
    return (
      <Box>
        <PageHeader title="Randevular" />
        <EmptyState label="Size ait işletme bulunamadı." />
      </Box>
    );
  }

  return (
    <Box>
      <PageHeader title="Randevular" />

      <Stack
        direction={{ xs: "column", md: "row" }}
        spacing={2}
        sx={{ mb: 2, alignItems: { xs: "stretch", md: "center" }, flexWrap: "wrap" }}
      >
        <TextField
          select
          size="small"
          label="İşletme"
          value={businessId}
          onChange={(e) => setSelectedId(e.target.value)}
          disabled={businesses.length <= 1}
          sx={{ minWidth: 220 }}
        >
          {businesses.map((business) => (
            <MenuItem key={business.id} value={business.id}>
              {business.name}
            </MenuItem>
          ))}
        </TextField>
        <TextField
          select
          size="small"
          label="Durum"
          value={status}
          onChange={(e) => setParam("status", e.target.value)}
          sx={{ minWidth: 160 }}
        >
          <MenuItem value="">Tümü</MenuItem>
          {APPOINTMENT_STATUS_OPTIONS.map((option) => (
            <MenuItem key={option.value} value={option.value}>
              {option.label}
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
          sx={{ minWidth: 180 }}
        />
        <TextField
          size="small"
          type="date"
          label="Bitiş Tarihi"
          value={toDate}
          onChange={(e) => setParam("toDate", e.target.value)}
          slotProps={{ inputLabel: { shrink: true } }}
          sx={{ minWidth: 180 }}
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
        ariaLabel="işletme randevuları tablosu"
        onRefresh={() => void listQuery.refetch()}
        rowActions={
          canCancel
            ? (row) => (
                <Button size="small" color="error" onClick={() => setPending({ id: row.id })}>
                  İptal
                </Button>
              )
            : undefined
        }
      />

      <ReasonConfirmDialog
        open={pending !== null}
        title={pending ? `Randevu İptali: ${shortId(pending.id)}` : ""}
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
    </Box>
  );
}
