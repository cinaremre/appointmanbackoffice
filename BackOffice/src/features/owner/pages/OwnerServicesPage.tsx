import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Controller, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  MenuItem,
  Snackbar,
  Stack,
  TextField,
} from "@mui/material";
import { ApiError } from "../../../shared/api/client";
import { toQueryString } from "../../../shared/api/types";
import { DataTable } from "../../../shared/components/DataTable";
import type { DataTableColumn } from "../../../shared/components/DataTable";
import { EmptyState, ErrorState, LoadingState, PageHeader } from "../../../shared/components/states";
import { StatusBadge } from "../../../shared/components/StatusBadge";
import { useAuth } from "../../../app/providers/AuthContext";
import { OWNER_PERMISSIONS } from "../../../shared/constants/permissions";
import { ownerGetList, ownerPatchItem, useOwnedBusinesses } from "../api/ownerClient";
import type { OwnerServiceListItem } from "../types";
import { activeText, formatMoney } from "../types";

/**
 * Owner hizmetleri: işletme kapsamında hizmet listesi. Arama/aktiflik
 * filtreleri sunucuya gönderilir; düzenleme RHF + zod ile dialog içinde
 * yapılır ve PATCH ile kaydedilir. Fiyat/süre kuralları SUNUCUDA da uygulanır.
 */

const serviceEditSchema = z.object({
  name: z.string().trim().min(2, "Hizmet adı en az 2 karakter olmalı."),
  price: z
    .string()
    .trim()
    .regex(/^\d+(\.\d{1,2})?$/, "Fiyat sayısal olmalı (orn. 250 veya 250.50)."),
  durationMinutes: z.string().trim().regex(/^\d+$/, "Süre tam sayı (dakika) olmalı."),
});

type ServiceEditForm = z.infer<typeof serviceEditSchema>;

type Feedback = { severity: "success" | "error"; message: string } | null;

function toErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof ApiError) return error.userMessage;
  if (error instanceof Error) return error.message;
  return fallback;
}

const ACTIVE_OPTIONS = [
  { value: "", label: "Tümü" },
  { value: "true", label: "Aktif" },
  { value: "false", label: "Pasif" },
] as const;

export function OwnerServicesPage() {
  const queryClient = useQueryClient();
  const { has_permission } = useAuth();
  const canEdit = has_permission(OWNER_PERMISSIONS.services);

  const businessesQuery = useOwnedBusinesses();
  const businesses = businessesQuery.data?.items ?? [];
  const [selectedId, setSelectedId] = useState("");
  const businessId = selectedId || businesses[0]?.id || "";

  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [activeParam, setActiveParam] = useState("");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(50);
  const [editing, setEditing] = useState<OwnerServiceListItem | null>(null);
  const [feedback, setFeedback] = useState<Feedback>(null);

  const isActiveFilter = activeParam === "true" ? true : activeParam === "false" ? false : undefined;

  const servicesQuery = useQuery({
    queryKey: ["owner", "services", businessId, { search, activeParam, page, pageSize }],
    queryFn: ({ signal }) =>
      ownerGetList<OwnerServiceListItem>(
        `/businesses/${businessId}/services${toQueryString({
          search: search || undefined,
          isActive: isActiveFilter,
          page,
          pageSize,
        })}`,
        signal,
      ),
    enabled: businessId !== "",
  });

  const { control, handleSubmit, reset } = useForm<ServiceEditForm>({
    resolver: zodResolver(serviceEditSchema),
    defaultValues: { name: "", price: "", durationMinutes: "" },
  });

  // Düzenlenecek hizmet seçilince form alanları doldurulur.
  useEffect(() => {
    if (!editing) return;
    reset({
      name: editing.name,
      price: String(editing.price),
      durationMinutes: String(editing.durationMinutes),
    });
  }, [editing, reset]);

  const saveMutation = useMutation({
    mutationFn: ({ id, values }: { id: string; values: ServiceEditForm }) =>
      ownerPatchItem(`/businesses/${businessId}/services/${id}`, {
        name: values.name,
        price: Number(values.price),
        durationMinutes: Number(values.durationMinutes),
      }),
    onSuccess: () => {
      setEditing(null);
      setFeedback({ severity: "success", message: "Hizmet güncellendi." });
      void queryClient.invalidateQueries({ queryKey: ["owner", "services"] });
    },
    onError: (error: unknown) => {
      setFeedback({ severity: "error", message: toErrorMessage(error, "Güncelleme başarısız.") });
    },
  });

  const onEditSubmit = (values: ServiceEditForm): void => {
    if (!editing) return;
    saveMutation.mutate({ id: editing.id, values });
  };

  const columns: DataTableColumn<OwnerServiceListItem>[] = [
    { key: "name", header: "Hizmet", render: (row) => row.name },
    { key: "price", header: "Fiyat", render: (row) => formatMoney(row.price, row.currency) },
    { key: "durationMinutes", header: "Süre", render: (row) => `${row.durationMinutes} dk` },
    {
      key: "isActive",
      header: "Durum",
      render: (row) => <StatusBadge status={activeText(row.isActive)} />,
    },
  ];

  if (businessesQuery.isLoading) return <LoadingState />;
  if (businessesQuery.isError) return <ErrorState error={businessesQuery.error} />;
  if (businesses.length === 0) {
    return (
      <Box>
        <PageHeader title="Hizmetler" />
        <EmptyState label="Size ait işletme bulunamadı." />
      </Box>
    );
  }

  return (
    <Box>
      <PageHeader title="Hizmetler" />
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
          size="small"
          label="Arama"
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
          sx={{ minWidth: 220 }}
        />
        <TextField
          select
          size="small"
          label="Aktiflik"
          value={activeParam}
          onChange={(e) => {
            setActiveParam(e.target.value);
            setPage(1);
          }}
          sx={{ minWidth: 140 }}
        >
          {ACTIVE_OPTIONS.map((option) => (
            <MenuItem key={option.value} value={option.value}>
              {option.label}
            </MenuItem>
          ))}
        </TextField>
        <Button
          variant="outlined"
          onClick={() => {
            setSearch(searchInput.trim());
            setPage(1);
          }}
        >
          Ara
        </Button>
      </Stack>

      <DataTable
        columns={columns}
        rows={servicesQuery.data?.items}
        loading={servicesQuery.isLoading}
        error={servicesQuery.error}
        page={page}
        pageSize={pageSize}
        totalCount={servicesQuery.data?.pagination.totalCount ?? 0}
        onPageChange={(nextPage) => setPage(nextPage)}
        onPageSizeChange={(nextSize) => {
          setPageSize(nextSize);
          setPage(1);
        }}
        emptyLabel="Hizmet bulunamadı."
        ariaLabel="işletme hizmetleri tablosu"
        onRefresh={() => void servicesQuery.refetch()}
        rowActions={
          canEdit
            ? (row) => (
                <Button size="small" onClick={() => setEditing(row)}>
                  Düzenle
                </Button>
              )
            : undefined
        }
      />

      <Dialog
        open={editing !== null}
        onClose={saveMutation.isPending ? undefined : () => setEditing(null)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Hizmeti Düzenle</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            <Controller
              control={control}
              name="name"
              render={({ field, fieldState }) => (
                <TextField
                  label="Hizmet Adı"
                  fullWidth
                  value={field.value}
                  onChange={field.onChange}
                  onBlur={field.onBlur}
                  error={fieldState.error !== undefined}
                  helperText={fieldState.error?.message ?? "Hizmetin görünen adı."}
                />
              )}
            />
            <Controller
              control={control}
              name="price"
              render={({ field, fieldState }) => (
                <TextField
                  label="Fiyat"
                  fullWidth
                  value={field.value}
                  onChange={field.onChange}
                  onBlur={field.onBlur}
                  error={fieldState.error !== undefined}
                  helperText={fieldState.error?.message ?? "Satış fiyatı (TL)."}
                />
              )}
            />
            <Controller
              control={control}
              name="durationMinutes"
              render={({ field, fieldState }) => (
                <TextField
                  label="Süre (dakika)"
                  fullWidth
                  value={field.value}
                  onChange={field.onChange}
                  onBlur={field.onBlur}
                  error={fieldState.error !== undefined}
                  helperText={fieldState.error?.message ?? "Hizmetin tamamlanma süresi."}
                />
              )}
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditing(null)} disabled={saveMutation.isPending}>
            Vazgeç
          </Button>
          <Button
            variant="contained"
            disabled={saveMutation.isPending}
            onClick={handleSubmit(onEditSubmit)}
          >
            {saveMutation.isPending ? "Kaydediliyor..." : "Kaydet"}
          </Button>
        </DialogActions>
      </Dialog>

      <Snackbar
        open={feedback !== null}
        autoHideDuration={5000}
        onClose={() => setFeedback(null)}
        anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
      >
        {feedback !== null ? (
          <Alert severity={feedback.severity} variant="filled" onClose={() => setFeedback(null)}>
            {feedback.message}
          </Alert>
        ) : undefined}
      </Snackbar>
    </Box>
  );
}
