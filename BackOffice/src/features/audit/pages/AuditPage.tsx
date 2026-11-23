import {
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  TextField,
  Typography,
  useMediaQuery,
  useTheme,
} from "@mui/material";
import { format } from "date-fns";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import type { FormEvent } from "react";
import { useSearchParams } from "react-router-dom";
import { getList } from "../../../shared/api/client";
import { toQueryString } from "../../../shared/api/types";
import { DataTable } from "../../../shared/components/DataTable";
import type { DataTableColumn } from "../../../shared/components/DataTable";
import { PageHeader } from "../../../shared/components/states";
import type { AuditLog } from "../types";

/**
 * Denetim kayıtları sayfası (doküman Modül 21): filtreler URL query ile
 * senkron çalışır; sayfa yenilense veya bağlantı paylasilsa aynı görünüm
 * kurulur. Detay dialog'unda değerler JSON blokları olarak gösterilir.
 */

const DEFAULT_PAGE_SIZE = 50;

/** ISO tarih -> "dd.MM.yyyy HH:mm"; geçersizse ham değer korunur. */
function formatDateTime(value?: string | null): string {
  if (!value) return "-";
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : format(date, "dd.MM.yyyy HH:mm");
}

/** GUID kısaltması; tablolarda uzun kimlikleri okunur tutar. */
function shortId(value?: string | null): string {
  return value ? value.slice(0, 8) : "-";
}

/**
 * Sunucudan gelen JSON betimlemesini okunur blokta gösterir. Metin geçerli
 * JSON değilse ham haliyle gösterilir; maskeleme sunucu tarafında yapılır.
 */
function JsonBlock({ title, value }: { title: string; value?: string | null }) {
  if (!value) return null;
  let text = value;
  try {
    text = JSON.stringify(JSON.parse(value), null, 2);
  } catch {
    // JSON parse edilemedi; ham metin gösterilir.
  }
  return (
    <Box sx={{ mt: 2 }}>
      <Typography variant="subtitle2" gutterBottom>
        {title}
      </Typography>
      <Box
        component="pre"
        sx={{ m: 0, p: 1.5, borderRadius: 1, bgcolor: "grey.100", overflowX: "auto", fontSize: 12 }}
      >
        {text}
      </Box>
    </Box>
  );
}

const auditColumns: DataTableColumn<AuditLog>[] = [
  { key: "createdAt", header: "Tarih", render: (row) => formatDateTime(row.createdAtUtc) },
  { key: "actor", header: "Aktör", render: (row) => shortId(row.actorUserId) },
  { key: "role", header: "Rol", render: (row) => row.actorRole },
  { key: "entity", header: "Varlık", render: (row) => row.entityType },
  { key: "action", header: "İşlem", render: (row) => row.action },
  { key: "business", header: "İşletme", render: (row) => shortId(row.businessId) },
];

export function AuditPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const theme = useTheme();
  const is_mobile = useMediaQuery(theme.breakpoints.down("sm"));

  // URL'den okunan aktif filtre değerleri.
  const actorUserIdParam = searchParams.get("actorUserId") ?? "";
  const businessIdParam = searchParams.get("businessId") ?? "";
  const entityTypeParam = searchParams.get("entityType") ?? "";
  const fromDateParam = searchParams.get("fromDate") ?? "";
  const toDateParam = searchParams.get("toDate") ?? "";
  const page = Math.max(1, Number(searchParams.get("page")) || 1);
  const pageSize = Math.max(1, Number(searchParams.get("pageSize")) || DEFAULT_PAGE_SIZE);

  // Form alanalri yerel state'te tutulur; "Filtrele" ile URL'e yazılır.
  const [actorUserId, setActorUserId] = useState(actorUserIdParam);
  const [businessId, setBusinessId] = useState(businessIdParam);
  const [entityType, setEntityType] = useState(entityTypeParam);
  const [fromDate, setFromDate] = useState(fromDateParam);
  const [toDate, setToDate] = useState(toDateParam);

  // Detay dialog'u seçili kayıtla açılır; veri liste öğesinde tamdir.
  const [selectedLog, setSelectedLog] = useState<AuditLog | null>(null);

  const logs = useQuery({
    queryKey: [
      "audit",
      "logs",
      {
        actorUserId: actorUserIdParam,
        businessId: businessIdParam,
        entityType: entityTypeParam,
        fromDate: fromDateParam,
        toDate: toDateParam,
        page,
        pageSize,
      },
    ],
    queryFn: ({ signal }) =>
      getList<AuditLog>(
        `/audit-logs${toQueryString({
          actorUserId: actorUserIdParam,
          businessId: businessIdParam,
          entityType: entityTypeParam,
          fromDate: fromDateParam,
          toDate: toDateParam,
          page,
          pageSize,
        })}`,
        signal,
      ),
  });

  /** Filtreleri URL query'ye yazar; sayfa bastan başlar. */
  const applyFilters = (event: FormEvent) => {
    event.preventDefault();
    const params = new URLSearchParams();
    const entries: Array<[string, string]> = [
      ["actorUserId", actorUserId.trim()],
      ["businessId", businessId.trim()],
      ["entityType", entityType.trim()],
      ["fromDate", fromDate],
      ["toDate", toDate],
    ];
    for (const [key, value] of entries) {
      if (value) params.set(key, value);
    }
    setSearchParams(params, { replace: true });
  };

  const clearFilters = () => {
    setActorUserId("");
    setBusinessId("");
    setEntityType("");
    setFromDate("");
    setToDate("");
    setSearchParams(new URLSearchParams(), { replace: true });
  };

  const handlePageChange = (newPage: number) => {
    const params = new URLSearchParams(searchParams);
    if (newPage > 1) params.set("page", String(newPage));
    else params.delete("page");
    setSearchParams(params, { replace: true });
  };

  const handlePageSizeChange = (newPageSize: number) => {
    const params = new URLSearchParams(searchParams);
    params.set("pageSize", String(newPageSize));
    params.delete("page");
    setSearchParams(params, { replace: true });
  };

  return (
    <Box>
      <PageHeader title="Denetim Kayıtları" />

      <Box component="form" onSubmit={applyFilters} sx={{ mb: 2 }} aria-label="denetim filtreleri">
        <Stack
          direction={{ xs: "column", md: "row" }}
          spacing={2}
          useFlexGap
          sx={{ flexWrap: "wrap", alignItems: { xs: "stretch", md: "center" } }}
        >
          <TextField
            label="Aktör ID (UUID)"
            value={actorUserId}
            onChange={(e) => setActorUserId(e.target.value)}
            size="small"
            sx={{ minWidth: { xs: 0, md: 240 } }}
          />
          <TextField
            label="İşletme ID (UUID)"
            value={businessId}
            onChange={(e) => setBusinessId(e.target.value)}
            size="small"
            sx={{ minWidth: { xs: 0, md: 240 } }}
          />
          <TextField
            label="Varlık Tipi"
            value={entityType}
            onChange={(e) => setEntityType(e.target.value)}
            size="small"
            sx={{ minWidth: { xs: 0, md: 180 } }}
          />
          <TextField
            label="Başlangıç"
            type="date"
            value={fromDate}
            onChange={(e) => setFromDate(e.target.value)}
            size="small"
            slotProps={{ inputLabel: { shrink: true } }}
            sx={{ minWidth: { xs: 0, md: 170 } }}
          />
          <TextField
            label="Bitiş"
            type="date"
            value={toDate}
            onChange={(e) => setToDate(e.target.value)}
            size="small"
            slotProps={{ inputLabel: { shrink: true } }}
            sx={{ minWidth: { xs: 0, md: 170 } }}
          />
          <Button type="submit" variant="contained">
            Filtrele
          </Button>
          <Button type="button" onClick={clearFilters}>
            Temizle
          </Button>
        </Stack>
      </Box>

      <DataTable<AuditLog>
        columns={auditColumns}
        rows={logs.data?.items}
        loading={logs.isLoading}
        error={logs.error}
        page={page}
        pageSize={pageSize}
        totalCount={logs.data?.pagination.totalCount ?? 0}
        onPageChange={handlePageChange}
        onPageSizeChange={handlePageSizeChange}
        emptyLabel="Denetim kaydı bulunamadı."
        ariaLabel="denetim kayıtları"
        onRefresh={() => logs.refetch()}
        rowActions={(row) => (
          <Button size="small" onClick={() => setSelectedLog(row)}>
            Detay
          </Button>
        )}
      />

      {/* Kayıt detayı: değerler JSON bloklarında, destek alanları açıkça etiketli. */}
      <Dialog
        open={selectedLog !== null}
        onClose={() => setSelectedLog(null)}
        fullWidth
        maxWidth="md"
        fullScreen={is_mobile}
      >
        <DialogTitle>Denetim Kaydı Detayı</DialogTitle>
        <DialogContent>
          {selectedLog ? (
            <Box sx={{ pt: 1 }}>
              <Stack spacing={0.5}>
                <Typography variant="body1">
                  <strong>{selectedLog.action}</strong> — {selectedLog.entityType}
                </Typography>
                <Typography variant="body2">
                  Aktör: {shortId(selectedLog.actorUserId)} ({selectedLog.actorRole})
                </Typography>
                <Typography variant="body2">
                  İşletme: {selectedLog.businessId ?? "-"}
                </Typography>
                <Typography variant="body2">
                  Varlık: {selectedLog.entityType} / {selectedLog.entityId}
                </Typography>
                <Typography variant="body2">
                  Tarih: {formatDateTime(selectedLog.createdAtUtc)}
                </Typography>
                <Typography variant="body2">Gerekçe: {selectedLog.reason ?? "-"}</Typography>
                <Typography variant="body2">
                  Korelasyon Kimliği: {selectedLog.correlationId ?? "-"}
                </Typography>
                <Typography variant="body2">
                  IP Adresi: {selectedLog.ipAddress ?? "-"}
                </Typography>
              </Stack>

              <JsonBlock title="Önceki Değerler" value={selectedLog.oldValues} />
              <JsonBlock title="Yeni Değerler" value={selectedLog.newValues} />

              <Typography variant="caption" color="text.secondary" sx={{ display: "block", mt: 2 }}>
                IP adresi ve korelasyon kimliği destek talepleri için gösterilir. Gerekçe ve
                değer betimlemeleri sunucuda sınırlandığından burada olduğu gibi sunulur.
              </Typography>
            </Box>
          ) : null}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setSelectedLog(null)}>Kapat</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
