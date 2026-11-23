import {
  Box,
  Button,
  Card,
  CardContent,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid,
  MenuItem,
  Stack,
  Tab,
  Tabs,
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
import { format } from "date-fns";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { getItem, getList } from "../../../shared/api/client";
import { toQueryString } from "../../../shared/api/types";
import { DataTable } from "../../../shared/components/DataTable";
import type { DataTableColumn } from "../../../shared/components/DataTable";
import { StatusBadge } from "../../../shared/components/StatusBadge";
import {
  EmptyState,
  ErrorState,
  LoadingState,
  PageHeader,
} from "../../../shared/components/states";
import type {
  FailedOperations,
  FailedRowSample,
  ImportJob,
  ImportJobDetail,
  SystemHealth,
} from "../types";
import {
  IMPORT_ROW_STATUS_LABELS,
  IMPORT_STATUS_LABELS,
  IMPORT_STATUS_OPTIONS,
} from "../types";

/**
 * Operasyon izleme sayfası (doküman Modül 20): sistem sağlığı, aktarım
 * işleri ve başarısız operasyonlar. Tüm sayılar API'den gelir.
 */

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

function jobColumns(): DataTableColumn<ImportJob>[] {
  return [
    { key: "source", header: "Kaynak Sistem", render: (row) => row.sourceSystem },
    {
      key: "status",
      header: "Durum",
      render: (row) => (
        <StatusBadge status={IMPORT_STATUS_LABELS[row.status] ?? String(row.status)} />
      ),
    },
    { key: "total", header: "Toplam Satır", render: (row) => String(row.totalRows) },
    { key: "imported", header: "Aktarilan", render: (row) => String(row.importedRows) },
    { key: "failed", header: "Başarısız Satır", render: (row) => String(row.failedRows) },
    { key: "started", header: "Başlangıç", render: (row) => formatDateTime(row.startedAt) },
    { key: "completed", header: "Bitiş", render: (row) => formatDateTime(row.completedAt) },
  ];
}

export function OperationsPage() {
  const [tab, setTab] = useState(0);
  const theme = useTheme();
  const is_mobile = useMediaQuery(theme.breakpoints.down("sm"));

  // İşler filtreleri ve sayfalama.
  const [businessId, setBusinessId] = useState("");
  const [jobStatus, setJobStatus] = useState("");
  const [jobsPage, setJobsPage] = useState(1);
  const [jobsPageSize, setJobsPageSize] = useState(50);

  // Satır detay dialog'u hangi iş için açık.
  const [detailJobId, setDetailJobId] = useState<string | null>(null);

  // Sağlık verisi 30 sn taze kabul edilir (doküman 26.4).
  const health = useQuery({
    queryKey: ["operations", "health"],
    queryFn: ({ signal }) => getItem<SystemHealth>("/operations/health", signal),
    staleTime: 30_000,
  });

  const jobs = useQuery({
    queryKey: [
      "operations",
      "jobs",
      { businessId, status: jobStatus, page: jobsPage, pageSize: jobsPageSize },
    ],
    queryFn: ({ signal }) =>
      getList<ImportJob>(
        `/operations/jobs${toQueryString({
          businessId,
          status: jobStatus,
          page: jobsPage,
          pageSize: jobsPageSize,
        })}`,
        signal,
      ),
    enabled: tab === 0,
  });

  const jobDetail = useQuery({
    queryKey: ["operations", "job-detail", detailJobId],
    queryFn: ({ signal }) => getItem<ImportJobDetail>(`/operations/jobs/${detailJobId}`, signal),
    enabled: detailJobId !== null,
  });

  const failed = useQuery({
    queryKey: ["operations", "failed"],
    queryFn: ({ signal }) => getItem<FailedOperations>("/operations/failed", signal),
    enabled: tab === 1,
  });

  const healthCards = health.data
    ? [
        { label: "Bekleyen İş", value: String(health.data.pendingJobs) },
        { label: "İşlenen İş", value: String(health.data.runningJobs) },
        { label: "Başarısız İş", value: String(health.data.failedJobs) },
      ]
    : [];

  return (
    <Box>
      <PageHeader title="Operasyonlar" />

      {/* Sağlık kartları: durum rozeti + iş sayıları. */}
      {health.isLoading ? (
        <LoadingState />
      ) : health.isError ? (
        <ErrorState error={health.error} />
      ) : (
        <Grid container spacing={2} sx={{ mb: 3 }}>
          <Grid size={{ xs: 12, sm: 6, md: 3 }}>
            <Card>
              <CardContent>
                <Typography color="text.secondary" variant="body2" gutterBottom>
                  Sistem Durumu
                </Typography>
                <StatusBadge status={health.data!.status} />
                {health.data!.note ? (
                  <Typography variant="caption" color="text.secondary" sx={{ mt: 1, display: "block" }}>
                    {health.data!.note}
                  </Typography>
                ) : null}
              </CardContent>
            </Card>
          </Grid>
          {healthCards.map((card) => (
            <Grid size={{ xs: 12, sm: 6, md: 3 }} key={card.label}>
              <Card>
                <CardContent>
                  <Typography color="text.secondary" variant="body2" gutterBottom>
                    {card.label}
                  </Typography>
                  <Typography variant="h5">{card.value}</Typography>
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>
      )}

      <Tabs value={tab} onChange={(_, value: number) => setTab(value)} aria-label="operasyon sekmeleri">
        <Tab label="İşler" />
        <Tab label="Başarısız" />
      </Tabs>

      <Box sx={{ mt: 2 }}>
        {tab === 0 ? (
          <Box>
            <Stack direction="row" spacing={2} useFlexGap sx={{ mb: 2, flexWrap: "wrap" }}>
              <TextField
                label="İşletme ID (UUID, opsiyonel)"
                value={businessId}
                onChange={(e) => {
                  setBusinessId(e.target.value);
                  setJobsPage(1);
                }}
                size="small"
                sx={{ minWidth: { xs: "100%", sm: 280 } }}
              />
              <TextField
                select
                label="Durum"
                value={jobStatus}
                onChange={(e) => {
                  setJobStatus(e.target.value);
                  setJobsPage(1);
                }}
                size="small"
                sx={{ minWidth: { xs: "100%", sm: 170 } }}
              >
                <MenuItem value="">Tümü</MenuItem>
                {IMPORT_STATUS_OPTIONS.map((option) => (
                  <MenuItem key={option.value} value={String(option.value)}>
                    {option.label}
                  </MenuItem>
                ))}
              </TextField>
            </Stack>

            <DataTable<ImportJob>
              columns={jobColumns()}
              rows={jobs.data?.items}
              loading={jobs.isLoading}
              error={jobs.error}
              page={jobsPage}
              pageSize={jobsPageSize}
              totalCount={jobs.data?.pagination.totalCount ?? 0}
              onPageChange={setJobsPage}
              onPageSizeChange={setJobsPageSize}
              emptyLabel="Aktarım işi bulunamadı."
              ariaLabel="aktarım işleri"
              onRefresh={() => jobs.refetch()}
              rowActions={(row) => (
                <Button size="small" onClick={() => setDetailJobId(row.id)}>
                  Detay
                </Button>
              )}
            />
          </Box>
        ) : null}

        {tab === 1 ? (
          <Box>
            {failed.isLoading ? (
              <LoadingState />
            ) : failed.isError ? (
              <ErrorState error={failed.error} />
            ) : failed.data && failed.data.failedJobs.length > 0 ? (
              <Box>
                <DataTable<ImportJob>
                  columns={jobColumns()}
                  rows={failed.data.failedJobs}
                  loading={false}
                  error={null}
                  page={1}
                  pageSize={50}
                  totalCount={failed.data.failedJobs.length}
                  onPageChange={() => {}}
                  onPageSizeChange={() => {}}
                  emptyLabel="Başarısız iş bulunamadı."
                  ariaLabel="başarısız işler"
                  onRefresh={() => failed.refetch()}
                />
                {failed.data.failedRows.length > 0 ? (
                  <Box sx={{ mt: 3 }}>
                    <Typography variant="h6" component="h3" sx={{ mb: 1 }}>
                      Başarısız Satır Ornekleri (ilk 50)
                    </Typography>
                    <TableContainer sx={{ maxHeight: 360 }}>
                      <Table size="small" aria-label="başarısız satırlar">
                        <TableHead>
                          <TableRow>
                            <TableCell>İş</TableCell>
                            <TableCell>Satır No</TableCell>
                            <TableCell>Kayıt Anahtarı</TableCell>
                            <TableCell>Hata</TableCell>
                          </TableRow>
                        </TableHead>
                        <TableBody>
                          {failed.data.failedRows.map((row: FailedRowSample) => (
                            <TableRow key={`${row.importJobId}-${row.rowNumber}`}>
                              <TableCell>{shortId(row.importJobId)}</TableCell>
                              <TableCell>{row.rowNumber}</TableCell>
                              <TableCell>{row.sourceRecordKey ?? "-"}</TableCell>
                              <TableCell>{row.errorDetails ?? "-"}</TableCell>
                            </TableRow>
                          ))}
                        </TableBody>
                      </Table>
                    </TableContainer>
                  </Box>
                ) : null}
              </Box>
            ) : (
              <EmptyState label="Başarısız iş bulunamadı." />
            )}
          </Box>
        ) : null}
      </Box>

      {/* İş detay dialog'u: iş özeti + satır sonuçları (ilk 200). */}
      <Dialog
        open={detailJobId !== null}
        onClose={() => setDetailJobId(null)}
        fullWidth
        maxWidth="md"
        fullScreen={is_mobile}
      >
        <DialogTitle>Aktarım İşi Detayı</DialogTitle>
        <DialogContent>
          {jobDetail.isLoading ? (
            <LoadingState />
          ) : jobDetail.isError ? (
            <ErrorState error={jobDetail.error} />
          ) : jobDetail.data ? (
            <Box sx={{ pt: 1 }}>
              <Stack spacing={0.5} sx={{ mb: 2 }}>
                <Typography variant="body1">
                  <strong>{jobDetail.data.job.sourceSystem}</strong>{" "}
                  <StatusBadge
                    status={IMPORT_STATUS_LABELS[jobDetail.data.job.status] ?? String(jobDetail.data.job.status)}
                  />
                </Typography>
                <Typography variant="body2">
                  İşletme: {shortId(jobDetail.data.job.businessId)}
                </Typography>
                <Typography variant="body2">
                  Satırlar: {jobDetail.data.job.importedRows} aktarılan /{" "}
                  {jobDetail.data.job.failedRows} başarısız / {jobDetail.data.job.totalRows} toplam
                </Typography>
                <Typography variant="body2">
                  Başlangıç: {formatDateTime(jobDetail.data.job.startedAt)} — Bitiş:{" "}
                  {formatDateTime(jobDetail.data.job.completedAt)}
                </Typography>
              </Stack>

              {jobDetail.data.rows.length > 0 ? (
                <TableContainer sx={{ maxHeight: 360 }}>
                  <Table size="small" aria-label="iş satır sonuçları">
                    <TableHead>
                      <TableRow>
                        <TableCell>Satır No</TableCell>
                        <TableCell>Kayıt Anahtarı</TableCell>
                        <TableCell>Durum</TableCell>
                        <TableCell>Hata</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {jobDetail.data.rows.map((row) => (
                        <TableRow key={row.id}>
                          <TableCell>{row.rowNumber}</TableCell>
                          <TableCell>{row.sourceRecordKey ?? "-"}</TableCell>
                          <TableCell>
                            <StatusBadge
                              status={IMPORT_ROW_STATUS_LABELS[row.status] ?? String(row.status)}
                            />
                          </TableCell>
                          <TableCell>{row.errorDetails ?? "-"}</TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>
              ) : (
                <EmptyState label="Satır sonucu bulunamadı." />
              )}
            </Box>
          ) : null}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDetailJobId(null)}>Kapat</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
