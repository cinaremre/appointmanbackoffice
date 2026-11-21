import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Controller, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Grid,
  MenuItem,
  Snackbar,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { ApiError } from "../../../shared/api/client";
import { EmptyState, ErrorState, LoadingState, PageHeader } from "../../../shared/components/states";
import { StatusBadge } from "../../../shared/components/StatusBadge";
import { useAuth } from "../../../app/providers/AuthContext";
import { OWNER_PERMISSIONS } from "../../../shared/constants/permissions";
import { ownerGetItem, ownerPatchItem, useOwnedBusinesses } from "../api/ownerClient";
import type { OwnedBusinessDetail } from "../types";
import { activeText, formatDateTime } from "../types";

/**
 * Owner isletme bilgileri sayfasi. Kapsam: kendi isletmesi. Kart bilgileri
 * salt okunur gosterilir; ad/aciklama duzenlemesi RHF + zod ile yapilir ve
 * PATCH ile gonderilir. Nihai yetki kontrolu SUNUCUDADIR.
 */

const businessEditSchema = z.object({
  name: z.string().trim().min(3, "Isletme adi en az 3 karakter olmali."),
  description: z.string().trim().max(1000, "Aciklama en fazla 1000 karakter olabilir."),
});

type BusinessEditForm = z.infer<typeof businessEditSchema>;

type Feedback = { severity: "success" | "error"; message: string } | null;

function toErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof ApiError) return error.userMessage;
  if (error instanceof Error) return error.message;
  return fallback;
}

export function OwnerBusinessPage() {
  const queryClient = useQueryClient();
  const { has_permission } = useAuth();
  const canEdit = has_permission(OWNER_PERMISSIONS.business);

  const businessesQuery = useOwnedBusinesses();
  const businesses = businessesQuery.data?.items ?? [];
  const [selectedId, setSelectedId] = useState("");
  const [feedback, setFeedback] = useState<Feedback>(null);

  // Tek isletme otomatik secilir.
  const businessId = selectedId || businesses[0]?.id || "";

  const detailQuery = useQuery({
    queryKey: ["owner", "business", "detail", businessId],
    queryFn: ({ signal }) => ownerGetItem<OwnedBusinessDetail>(`/businesses/${businessId}`, signal),
    enabled: businessId !== "",
  });

  const patchMutation = useMutation({
    mutationFn: (values: BusinessEditForm) => ownerPatchItem(`/businesses/${businessId}`, values),
    onSuccess: () => {
      setFeedback({ severity: "success", message: "Isletme bilgileri guncellendi." });
      void queryClient.invalidateQueries({ queryKey: ["owner", "businesses"] });
      void queryClient.invalidateQueries({ queryKey: ["owner", "business", "detail", businessId] });
    },
    onError: (error: unknown) => {
      setFeedback({ severity: "error", message: toErrorMessage(error, "Guncelleme basarisiz.") });
    },
  });

  const { control, handleSubmit, reset } = useForm<BusinessEditForm>({
    resolver: zodResolver(businessEditSchema),
    defaultValues: { name: "", description: "" },
  });

  // Detay yuklenince form alanlari sunucu verisiyle esitlenir.
  useEffect(() => {
    const detail = detailQuery.data;
    if (!detail) return;
    reset({ name: detail.name, description: detail.description ?? "" });
  }, [detailQuery.data, reset]);

  const onEditSubmit = (values: BusinessEditForm): void => {
    patchMutation.mutate(values);
  };

  if (businessesQuery.isLoading) return <LoadingState />;
  if (businessesQuery.isError) return <ErrorState error={businessesQuery.error} />;
  if (businesses.length === 0) {
    return (
      <Box>
        <PageHeader title="Isletme Bilgileri" />
        <EmptyState label="Size ait isletme bulunamadi." />
      </Box>
    );
  }

  const detail = detailQuery.data;

  const infoFields: Array<{ label: string; value: string }> = detail
    ? [
        { label: "Sehir (cityId)", value: detail.cityId },
        {
          label: "Puan",
          value: `${detail.rating.toFixed(1)} (${detail.totalReviews} yorum)`,
        },
        { label: "Aktif Hizmet Sayisi", value: String(detail.activeServiceCount) },
        { label: "Calisan Sayisi", value: String(detail.employeeCount) },
        { label: "Bugunku Randevu", value: String(detail.todaysAppointmentCount) },
        { label: "Kayit Tarihi", value: formatDateTime(detail.createdAt) },
      ]
    : [];

  return (
    <Box>
      <PageHeader title="Isletme Bilgileri" />
      <Stack
        direction={{ xs: "column", md: "row" }}
        spacing={2}
        sx={{ mb: 2, alignItems: { xs: "stretch", md: "center" }, flexWrap: "wrap" }}
      >
        <TextField
          select
          size="small"
          label="Isletme"
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
      </Stack>

      {detailQuery.isLoading ? (
        <LoadingState />
      ) : detailQuery.isError ? (
        <ErrorState error={detailQuery.error} />
      ) : detail ? (
        <>
          <Box sx={{ display: "flex", alignItems: "center", gap: 1, flexWrap: "wrap", mb: 2 }}>
            <StatusBadge status={activeText(detail.isActive)} />
            <Typography variant="body1">{detail.name}</Typography>
          </Box>
          <Grid container spacing={2}>
            {infoFields.map((field) => (
              <Grid size={{ xs: 12, sm: 6, md: 4 }} key={field.label}>
                <Typography variant="body2" color="text.secondary">
                  {field.label}
                </Typography>
                <Typography variant="body1">{field.value}</Typography>
              </Grid>
            ))}
          </Grid>
          {detail.description ? (
            <Box sx={{ mt: 2 }}>
              <Typography variant="body2" color="text.secondary">
                Aciklama
              </Typography>
              <Typography variant="body1">{detail.description}</Typography>
            </Box>
          ) : null}
          {canEdit ? (
            <Card sx={{ mt: 3 }}>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Bilgileri Duzenle
                </Typography>
                <form onSubmit={handleSubmit(onEditSubmit)} noValidate>
                  <Grid container spacing={2}>
                    <Grid size={{ xs: 12 }}>
                      <Controller
                        control={control}
                        name="name"
                        render={({ field, fieldState }) => (
                          <TextField
                            label="Isletme Adi"
                            fullWidth
                            value={field.value}
                            onChange={field.onChange}
                            onBlur={field.onBlur}
                            error={fieldState.error !== undefined}
                            helperText={fieldState.error?.message ?? "Isletmenin gorunen adi."}
                          />
                        )}
                      />
                    </Grid>
                    <Grid size={{ xs: 12 }}>
                      <Controller
                        control={control}
                        name="description"
                        render={({ field, fieldState }) => (
                          <TextField
                            label="Aciklama"
                            fullWidth
                            multiline
                            minRows={3}
                            value={field.value}
                            onChange={field.onChange}
                            onBlur={field.onBlur}
                            error={fieldState.error !== undefined}
                            helperText={
                              fieldState.error?.message ?? "Isletme hakkinda kisa aciklama."
                            }
                          />
                        )}
                      />
                    </Grid>
                    <Grid size={{ xs: 12 }}>
                      <Button type="submit" variant="contained" disabled={patchMutation.isPending}>
                        {patchMutation.isPending ? "Kaydediliyor..." : "Kaydet"}
                      </Button>
                    </Grid>
                  </Grid>
                </form>
              </CardContent>
            </Card>
          ) : null}
        </>
      ) : null}

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
