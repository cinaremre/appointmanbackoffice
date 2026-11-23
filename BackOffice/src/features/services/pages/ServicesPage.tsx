import { useEffect, useMemo, useState } from "react";
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  FormControlLabel,
  FormLabel,
  InputLabel,
  MenuItem,
  Radio,
  RadioGroup,
  Select,
  Snackbar,
  Stack,
  Step,
  StepLabel,
  Stepper,
  TextField,
  Typography,
  useMediaQuery,
  useTheme,
} from "@mui/material";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { useSearchParams } from "react-router-dom";
import { ApiError, getList, patchItem, postItem } from "../../../shared/api/client";
import { toQueryString } from "../../../shared/api/types";
import { DataTable } from "../../../shared/components/DataTable";
import type { DataTableColumn } from "../../../shared/components/DataTable";
import { PageHeader } from "../../../shared/components/states";
import { StatusBadge } from "../../../shared/components/StatusBadge";
import { PERMISSIONS } from "../../../shared/constants/permissions";
import { useAuth } from "../../../app/providers/AuthContext";
import type {
  BulkOperationPreviewDto,
  BulkOperationResultDto,
  BulkOperationType,
  ServiceDto,
} from "../types";

/**
 * Servis yönetimi (doküman 11): DataTable + URL senkron filtreler,
 * küçük düzenleme dialog'u ve 4 adimli toplu işlem wizard'ı (doküman 25).
 */

type NotifySeverity = "success" | "warning" | "error";
type Notify = (message: string, severity?: NotifySeverity) => void;

function errorMessage(error: unknown): string {
  return error instanceof ApiError ? error.userMessage : "Beklenmedik bir hata oluştu. Lütfen tekrar deneyin.";
}

/** Düzenleme formu şeması: alanlar string gelir, gönderirken sayıya çevrilir. */
const serviceEditSchema = z.object({
  name: z
    .string()
    .trim()
    .min(2, "Hizmet adı en az 2 karakter olmalı.")
    .max(100, "Hizmet adı en fazla 100 karakter olabilir."),
  price: z
    .string()
    .trim()
    .refine((value) => /^[0-9]+(\.[0-9]{1,2})?$/.test(value), "Geçerli bir fiyat girin (orn. 250.50)."),
  durationMinutes: z
    .string()
    .trim()
    .refine(
      (value) => /^[0-9]+$/.test(value) && Number(value) >= 5 && Number(value) <= 1440,
      "Süre 5-1440 dk arasında tam sayı olmalı.",
    ),
});
type ServiceEditValues = z.infer<typeof serviceEditSchema>;

/** Servis düzenleme formu (PATCH). Mount anında satırın değerleriyle başlar. */
function ServiceEditForm({
  row,
  onClose,
  notify,
}: {
  row: ServiceDto;
  onClose: () => void;
  notify: Notify;
}) {
  const queryClient = useQueryClient();
  const theme = useTheme();
  const is_mobile = useMediaQuery(theme.breakpoints.down("sm"));
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ServiceEditValues>({
    resolver: zodResolver(serviceEditSchema),
    defaultValues: {
      name: row.name,
      price: String(row.price),
      durationMinutes: String(row.durationMinutes),
    },
  });

  const updateMutation = useMutation({
    mutationFn: (payload: { name: string; price: number; durationMinutes: number }) =>
      patchItem<ServiceDto>(`/services/${row.id}`, payload),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["services"] });
      notify("Hizmet güncellendi.");
      onClose();
    },
    onError: (error: unknown) => notify(errorMessage(error), "error"),
  });

  const submit = handleSubmit((values) => {
    updateMutation.mutate({
      name: values.name,
      price: Number(values.price),
      durationMinutes: Number(values.durationMinutes),
    });
  });

  return (
    <Dialog
      open
      onClose={updateMutation.isPending ? undefined : onClose}
      maxWidth="sm"
      fullWidth
      fullScreen={is_mobile}
    >
      <DialogTitle>Servisi Düzenle</DialogTitle>
      <form onSubmit={submit} noValidate>
        <DialogContent>
          <TextField
            label="Hizmet Adı"
            fullWidth
            margin="dense"
            error={!!errors.name}
            helperText={errors.name?.message}
            {...register("name")}
          />
          <TextField
            label="Fiyat"
            fullWidth
            margin="dense"
            error={!!errors.price}
            helperText={errors.price?.message}
            {...register("price")}
          />
          <TextField
            label="Süre (dakika)"
            fullWidth
            margin="dense"
            error={!!errors.durationMinutes}
            helperText={errors.durationMinutes?.message}
            {...register("durationMinutes")}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose} disabled={updateMutation.isPending}>
            Vazgeç
          </Button>
          <Button type="submit" variant="contained" disabled={updateMutation.isPending}>
            {updateMutation.isPending ? "Kaydediliyor..." : "Kaydet"}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
}

const BULK_STEP_LABELS = ["İşlem Türü", "Servis ID'leri", "Önizleme", "Sonuç"];

const BULK_OPERATION_OPTIONS: Array<{ value: BulkOperationType; label: string; description: string }> = [
  {
    value: "DeactivateServices",
    label: "Servisleri Pasifleştir",
    description: "Seçilen servisler işletme sayfasında satın alınamaz hale gelir.",
  },
  {
    value: "ActivateServices",
    label: "Servisleri Aktifleştir",
    description: "Seçilen servisler yeniden satın alınabilir hale gelir.",
  },
  {
    value: "ChangeServiceCategory",
    label: "Servis Kategorisini Değiştir",
    description: "Seçilen servisler belirttiğiniz hedef kategoriye taşınır.",
  },
];

/**
 * Toplu işlem wizard'ı (doküman 25): tür seçimi -> ID listesi ->
 * dryRun önizleme -> onay ve execute. Mount anında temiz başlar.
 * Sunucu 2000 üstü istekleri 409 ile reddeder; hata mesajı dialog
 * içinde gösterilir.
 */
function BulkOperationForm({ onClose, notify }: { onClose: () => void; notify: Notify }) {
  const queryClient = useQueryClient();
  const theme = useTheme();
  // Mobilde sihirbaz tam ekran açılır; Stepper normal (dikey esikli) akar.
  const is_mobile = useMediaQuery(theme.breakpoints.down("sm"));
  const [activeStep, setActiveStep] = useState(0);
  const [operation, setOperation] = useState<BulkOperationType>("DeactivateServices");
  const [idsText, setIdsText] = useState("");
  const [targetCategoryId, setTargetCategoryId] = useState("");
  const [preview, setPreview] = useState<BulkOperationPreviewDto | null>(null);
  const [result, setResult] = useState<BulkOperationResultDto | null>(null);
  const [error, setError] = useState<string | null>(null);

  // Satır satır girilen GUID'leri ayikla: boşluk/virgul toleranslı, tekrarsiz.
  const serviceIds = useMemo(() => {
    const seen = new Set<string>();
    for (const token of idsText.split(/[\s,;]+/)) {
      const value = token.trim();
      if (value) seen.add(value);
    }
    return [...seen];
  }, [idsText]);

  const dryRunMutation = useMutation({
    mutationFn: () =>
      postItem<BulkOperationPreviewDto>("/services/bulk-operations", {
        operation,
        serviceIds,
        ...(operation === "ChangeServiceCategory" ? { targetServiceCategoryId: targetCategoryId.trim() } : {}),
        dryRun: true,
      }),
    onSuccess: (data) => {
      if (!data) {
        setError("Sunucu beklenmeyen bir yanıt döndü.");
        return;
      }
      setError(null);
      setPreview(data);
    },
    onError: (mutationError: unknown) => setError(errorMessage(mutationError)),
  });

  const executeMutation = useMutation({
    mutationFn: (operationId: string) =>
      postItem<BulkOperationResultDto>(`/services/bulk-operations/${operationId}/execute`),
    onSuccess: async (data) => {
      if (!data) {
        setError("Sunucu beklenmeyen bir yanıt döndü.");
        return;
      }
      setResult(data);
      setActiveStep(3);
      await queryClient.invalidateQueries({ queryKey: ["services"] });
      notify(
        data.failedCount > 0
          ? `Toplu işlem kismen başarılı: ${data.successCount} başarılı, ${data.failedCount} başarısız.`
          : `Toplu işlem tamamlandı: ${data.successCount} kayıt işlendi.`,
        data.failedCount > 0 ? "warning" : "success",
      );
    },
    onError: (mutationError: unknown) => setError(errorMessage(mutationError)),
  });

  const cancelMutation = useMutation({
    mutationFn: (operationId: string) => postItem(`/services/bulk-operations/${operationId}/cancel`),
    onSuccess: () => {
      notify("Toplu işlem iptal edildi.", "warning");
      onClose();
    },
    onError: (mutationError: unknown) => notify(errorMessage(mutationError), "error"),
  });

  const busy = dryRunMutation.isPending || executeMutation.isPending || cancelMutation.isPending;

  const handleNext = () => {
    if (activeStep === 0) {
      setError(null);
      setActiveStep(1);
      return;
    }
    if (activeStep === 1) {
      if (serviceIds.length === 0) {
        setError("En az bir geçerli servis ID girin.");
        return;
      }
      setError(null);
      setPreview(null);
      setResult(null);
      setActiveStep(2);
    }
  };

  const runDryRun = () => {
    if (operation === "ChangeServiceCategory" && !targetCategoryId.trim()) {
      setError("ChangeServiceCategory için hedef kategori ID zorunludur.");
      return;
    }
    setError(null);
    dryRunMutation.mutate();
  };

  return (
    <Dialog open onClose={busy ? undefined : onClose} maxWidth="md" fullWidth fullScreen={is_mobile}>
      <DialogTitle>Toplu Servis İşlemi</DialogTitle>
      <DialogContent>
        <Stepper
          activeStep={activeStep}
          sx={{
            my: 2,
            flexWrap: "wrap",
            "& .MuiStep-labelContainer": { overflow: "hidden" },
            "& .MuiStepLabel-label": { fontSize: { xs: "0.7rem", sm: "0.75rem" } },
          }}
        >
          {BULK_STEP_LABELS.map((label) => (
            <Step key={label}>
              <StepLabel>{label}</StepLabel>
            </Step>
          ))}
        </Stepper>

        {error ? (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        ) : null}

        {activeStep === 0 ? (
          <Stack spacing={2}>
            <FormControl>
              <FormLabel id="bulk-operation-label">İşlem Türü</FormLabel>
              <RadioGroup
                aria-labelledby="bulk-operation-label"
                value={operation}
                onChange={(event) => setOperation(event.target.value as BulkOperationType)}
              >
                {BULK_OPERATION_OPTIONS.map((option) => (
                  <FormControlLabel
                    key={option.value}
                    value={option.value}
                    control={<Radio />}
                    label={option.label}
                  />
                ))}
              </RadioGroup>
            </FormControl>
            <Typography variant="body2" color="text.secondary">
              {BULK_OPERATION_OPTIONS.find((option) => option.value === operation)?.description}
            </Typography>
            {operation === "ChangeServiceCategory" ? (
              <TextField
                label="Hedef Servis Kategori ID"
                fullWidth
                value={targetCategoryId}
                onChange={(event) => setTargetCategoryId(event.target.value)}
                helperText="ChangeServiceCategory işlemi için hedef kategori ID'si zorunludur."
              />
            ) : null}
          </Stack>
        ) : null}

        {activeStep === 1 ? (
          <TextField
            label="Servis ID Listesi"
            fullWidth
            multiline
            minRows={8}
            value={idsText}
            onChange={(event) => setIdsText(event.target.value)}
            placeholder={"00000000-0000-0000-0000-000000000001\n00000000-0000-0000-0000-000000000002"}
            helperText={`${serviceIds.length} benzersiz ID algılandı. Her satıra bir GUID yazın. Sunucu 2000 üstü istekleri reddeder (409).`}
          />
        ) : null}

        {activeStep === 2 ? (
          preview ? (
            <Stack spacing={2}>
              <Alert severity={preview.invalidIds.length > 0 ? "warning" : "success"}>
                Önizleme (dryRun): {preview.totalRecords} kayıt işleme alınacak.
                {preview.invalidIds.length > 0
                  ? ` ${preview.invalidIds.length} geçersiz ID reddedildi.`
                  : ""}
              </Alert>
              {preview.invalidIds.length > 0 ? (
                <Box
                  sx={{
                    maxHeight: 160,
                    overflow: "auto",
                    border: "1px solid",
                    borderColor: "divider",
                    borderRadius: 1,
                    p: 1,
                  }}
                >
                  <Typography variant="caption" color="text.secondary">
                    Geçersiz ID'ler:
                  </Typography>
                  {preview.invalidIds.map((invalidId) => (
                    <Typography key={invalidId} variant="caption" sx={{ display: "block", fontFamily: "monospace" }}>
                      {invalidId}
                    </Typography>
                  ))}
                </Box>
              ) : null}
            </Stack>
          ) : (
            <Button variant="outlined" onClick={runDryRun} disabled={dryRunMutation.isPending}>
              {dryRunMutation.isPending ? "Doğrulanıyor..." : "Önizlemeyi Çalıştır (dryRun)"}
            </Button>
          )
        ) : null}

        {activeStep === 3 && result ? (
          <Stack spacing={2}>
            <Alert severity={result.failedCount > 0 ? "warning" : "success"}>
              İşlem durumu: {result.status}. {result.successCount} kayıt başarıyla işlendi, {result.failedCount}{" "}
              kayıt başarısız.
            </Alert>
            {result.errorReport ? (
              <Box
                sx={{
                  maxHeight: 200,
                  overflow: "auto",
                  border: "1px solid",
                  borderColor: "divider",
                  borderRadius: 1,
                  p: 1,
                }}
              >
                <Typography component="pre" variant="caption" sx={{ whiteSpace: "pre-wrap", fontFamily: "monospace" }}>
                  {result.errorReport}
                </Typography>
              </Box>
            ) : null}
          </Stack>
        ) : null}
      </DialogContent>
      <DialogActions>
        {activeStep > 0 && activeStep < 3 ? (
          <Button onClick={() => setActiveStep(activeStep - 1)} disabled={busy}>
            Geri
          </Button>
        ) : null}
        {activeStep < 2 ? (
          <Button variant="contained" onClick={handleNext} disabled={activeStep === 1 && serviceIds.length === 0}>
            İleri
          </Button>
        ) : null}
        {activeStep < 2 ? (
          <Button onClick={onClose} disabled={busy}>
            Vazgeç
          </Button>
        ) : null}
        {activeStep === 2 && !preview ? (
          <Button variant="contained" onClick={runDryRun} disabled={dryRunMutation.isPending}>
            {dryRunMutation.isPending ? "Doğrulanıyor..." : "Önizlemeyi Çalıştır"}
          </Button>
        ) : null}
        {activeStep === 2 && preview ? (
          <>
            <Button
              color="warning"
              onClick={() => cancelMutation.mutate(preview.id)}
              disabled={executeMutation.isPending || cancelMutation.isPending}
            >
              İşlemi İptal Et
            </Button>
            <Button
              variant="contained"
              onClick={() => executeMutation.mutate(preview.id)}
              disabled={executeMutation.isPending}
            >
              {executeMutation.isPending ? "Uygulanıyor..." : "Onayla ve Uygula"}
            </Button>
          </>
        ) : null}
        {activeStep === 3 ? (
          <Button variant="contained" onClick={onClose}>
            Kapat
          </Button>
        ) : null}
      </DialogActions>
    </Dialog>
  );
}

export function ServicesPage() {
  const { has_permission } = useAuth();
  const queryClient = useQueryClient();
  const [searchParams, setSearchParams] = useSearchParams();

  const [snackbar, setSnackbar] = useState<{ open: boolean; message: string; severity: NotifySeverity }>({
    open: false,
    message: "",
    severity: "success",
  });
  const notify: Notify = (message, severity = "success") =>
    setSnackbar({ open: true, message, severity });

  const [editRow, setEditRow] = useState<ServiceDto | null>(null);
  const [wizardOpen, setWizardOpen] = useState(false);

  // Filtreler URL query'de yasar (doküman 22): geri/yenile uyumlu.
  const search = searchParams.get("search") ?? "";
  const isActive = searchParams.get("isActive") ?? "";
  const page = Math.max(1, Number(searchParams.get("page")) || 1);
  const pageSize = Math.min(200, Math.max(10, Number(searchParams.get("pageSize")) || 50));

  const [searchInput, setSearchInput] = useState(search);
  // URL dışarıdan değişirse (geri gitme) input'u render sırasında senkronla.
  const [lastSearch, setLastSearch] = useState(search);
  if (search !== lastSearch) {
    setLastSearch(search);
    setSearchInput(search);
  }

  const applyParams = (updates: Record<string, string>, resetPage = true) => {
    const next = new URLSearchParams(searchParams.toString());
    for (const [key, value] of Object.entries(updates)) {
      if (value === "") next.delete(key);
      else next.set(key, value);
    }
    if (resetPage) next.delete("page");
    setSearchParams(next, { replace: true });
  };

  // Arama kutusu debounced URL'e yazılır; her tuş vuruşunda istek atilmaz.
  useEffect(() => {
    if (searchInput === search) return;
    const timer = setTimeout(() => applyParams({ search: searchInput }), 400);
    return () => clearTimeout(timer);
  });

  const servicesQuery = useQuery({
    queryKey: ["services", { search, isActive, page, pageSize }],
    queryFn: (context) =>
      getList<ServiceDto>(`/services${toQueryString({ search, isActive, page, pageSize })}`, context.signal),
    placeholderData: keepPreviousData,
  });

  const canUpdate = has_permission(PERMISSIONS.serviceUpdate);
  const canBulk = has_permission(PERMISSIONS.serviceBulkUpdate);

  const columns: DataTableColumn<ServiceDto>[] = [
    { key: "name", header: "Hizmet Adı", render: (row) => row.name },
    {
      key: "businessId",
      header: "İşletme",
      render: (row) => (
        <Typography variant="body2" noWrap title={row.businessId} sx={{ fontFamily: "monospace", maxWidth: 220 }}>
          {row.businessId}
        </Typography>
      ),
    },
    {
      key: "price",
      header: "Fiyat",
      render: (row) => `${Number(row.price).toFixed(2)} ${row.currency}`,
    },
    { key: "duration", header: "Süre", render: (row) => `${row.durationMinutes} dk` },
    {
      key: "status",
      header: "Durum",
      render: (row) => <StatusBadge status={row.isActive ? "Aktif" : "Pasif"} />,
    },
  ];

  const closeSnackbar = (_event?: unknown, reason?: string) => {
    if (reason === "clickaway") return;
    setSnackbar((prev) => ({ ...prev, open: false }));
  };

  return (
    <Box>
      <PageHeader
        title="Hizmetler"
        actions={
          canBulk ? (
            <Button variant="contained" onClick={() => setWizardOpen(true)}>
              Toplu İşlem
            </Button>
          ) : null
        }
      />

      <Box sx={{ display: "flex", gap: 2, flexWrap: "wrap", alignItems: "center", mb: 2 }}>
        <TextField
          label="Ara"
          size="small"
          sx={{ minWidth: { xs: "100%", sm: 240 } }}
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
        <FormControl size="small" sx={{ minWidth: { xs: "100%", sm: 160 } }}>
          <InputLabel id="services-active-filter">Durum</InputLabel>
          <Select
            labelId="services-active-filter"
            label="Durum"
            value={isActive}
            onChange={(event) => applyParams({ isActive: event.target.value, search: searchInput })}
          >
            <MenuItem value="">Tümü</MenuItem>
            <MenuItem value="true">Aktif</MenuItem>
            <MenuItem value="false">Pasif</MenuItem>
          </Select>
        </FormControl>
      </Box>

      <DataTable
        ariaLabel="hizmetler tablosu"
        columns={columns}
        rows={servicesQuery.data?.items}
        loading={servicesQuery.isLoading}
        error={servicesQuery.error}
        page={page}
        pageSize={pageSize}
        totalCount={servicesQuery.data?.pagination.totalCount ?? 0}
        onPageChange={(value) => applyParams({ page: String(value) }, false)}
        onPageSizeChange={(value) => applyParams({ pageSize: String(value), page: "1" }, false)}
        emptyLabel="Hizmet bulunamadı."
        rowActions={
          canUpdate
            ? (row) => (
                <Button size="small" onClick={() => setEditRow(row)}>
                  Düzenle
                </Button>
              )
            : undefined
        }
        onRefresh={() => {
          void queryClient.invalidateQueries({ queryKey: ["services"] });
        }}
      />

      {editRow ? <ServiceEditForm key={editRow.id} row={editRow} onClose={() => setEditRow(null)} notify={notify} /> : null}
      {wizardOpen ? <BulkOperationForm onClose={() => setWizardOpen(false)} notify={notify} /> : null}

      <Snackbar
        open={snackbar.open}
        autoHideDuration={4000}
        onClose={closeSnackbar}
        anchorOrigin={{ vertical: "bottom", horizontal: "center" }}
      >
        <Alert severity={snackbar.severity} variant="filled" onClose={() => closeSnackbar()}>
          {snackbar.message}
        </Alert>
      </Snackbar>
    </Box>
  );
}
