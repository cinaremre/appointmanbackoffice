import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { format } from "date-fns";
import { useNavigate, useParams } from "react-router-dom";
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
  Snackbar,
  Tab,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Tabs,
  TextField,
  Typography,
} from "@mui/material";
import { ApiError, getItem, getList, patchItem, postItem } from "../../../shared/api/client";
import { EmptyState, ErrorState, LoadingState, PageHeader } from "../../../shared/components/states";
import { ReasonConfirmDialog } from "../../../shared/components/ReasonConfirmDialog";
import { StatusBadge } from "../../../shared/components/StatusBadge";
import { PERMISSIONS } from "../../../shared/constants/permissions";
import { useAuth } from "../../../app/providers/AuthContext";
import type { AuditEntry, BusinessDetail } from "../types";

/**
 * İŞLETME detay sayfası (doküman 9).
 * "Genel" ve "Audit Geçmişi" sekmeleri; askıya alma/aktifleştirme
 * gerekçe zorunlu onay diyaloguyla, düzenleme RHF + zod ile yapılır.
 */

const editSchema = z.object({
  name: z.string().trim().min(3, "İşletme adı en az 3 karakter olmalı."),
  description: z.string().trim().max(1000, "Açıklama en fazla 1000 karakter olabilir."),
});

type BusinessEditForm = z.infer<typeof editSchema>;

const SUSPEND_IMPACT =
  "Bu işletmeyi askıya almak yeni rezervasyonların görünürlüğünü etkileyebilir. Mevcut randevular işletme politikasına göre korunur.";
const ACTIVATE_IMPACT =
  "Bu işletmeyi aktifleştirmek işletmenin yeni rezervasyonlarda tekrar görünür olmasını sağlar. Mevcut randevular ve işletme politikası etkilenmez.";

type DetailTab = "general" | "audit";
type PendingStatusAction = "suspend" | "activate";
type Feedback = { severity: "success" | "error"; message: string } | null;

function formatDateTime(value: string): string {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : format(date, "dd.MM.yyyy HH:mm");
}

function toErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof ApiError) return error.userMessage;
  if (error instanceof Error) return error.message;
  return fallback;
}

export function BusinessDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { has_permission } = useAuth();

  const [tab, setTab] = useState<DetailTab>("general");
  const [pendingAction, setPendingAction] = useState<PendingStatusAction | null>(null);
  const [feedback, setFeedback] = useState<Feedback>(null);

  const detailQuery = useQuery({
    queryKey: ["businesses", "detail", id],
    queryFn: ({ signal }) => getItem<BusinessDetail>(`/businesses/${id}`, signal),
    enabled: id !== undefined,
  });

  // Audit geçmişi yalnızca ilgili sekme açıldığında çekilir.
  const auditQuery = useQuery({
    queryKey: ["businesses", "detail", id, "audit"],
    queryFn: ({ signal }) => getList<AuditEntry>(`/businesses/${id}/audit`, signal),
    enabled: id !== undefined && tab === "audit",
  });

  const patchMutation = useMutation({
    mutationFn: (values: BusinessEditForm) =>
      patchItem<BusinessDetail>(`/businesses/${id}`, values),
    onSuccess: () => {
      setFeedback({ severity: "success", message: "İşletme bilgileri güncellendi." });
      void queryClient.invalidateQueries({ queryKey: ["businesses"] });
    },
    onError: (error: unknown) => {
      setFeedback({ severity: "error", message: toErrorMessage(error, "Güncelleme başarısız.") });
    },
  });

  const suspendMutation = useMutation({
    mutationFn: (reason: string) => postItem(`/businesses/${id}/suspend`, { reason }),
    onSuccess: () => {
      setFeedback({ severity: "success", message: "İşletme askıya alındı." });
      setPendingAction(null);
      void queryClient.invalidateQueries({ queryKey: ["businesses"] });
    },
    onError: (error: unknown) => {
      setFeedback({ severity: "error", message: toErrorMessage(error, "Askıya alma başarısız.") });
    },
  });

  const activateMutation = useMutation({
    mutationFn: (reason: string) => postItem(`/businesses/${id}/activate`, { reason }),
    onSuccess: () => {
      setFeedback({ severity: "success", message: "İşletme aktifleştirildi." });
      setPendingAction(null);
      void queryClient.invalidateQueries({ queryKey: ["businesses"] });
    },
    onError: (error: unknown) => {
      setFeedback({
        severity: "error",
        message: toErrorMessage(error, "Aktifleştirme başarısız."),
      });
    },
  });

  const { control, handleSubmit, reset } = useForm<BusinessEditForm>({
    resolver: zodResolver(editSchema),
    defaultValues: { name: "", description: "" },
  });

  // Detay yüklenince form alanları sunucu verisiyle eşitlenir.
  useEffect(() => {
    const detail = detailQuery.data;
    if (!detail) return;
    reset({ name: detail.name, description: detail.description ?? "" });
  }, [detailQuery.data, reset]);

  const canUpdate = has_permission(PERMISSIONS.businessUpdate);
  const canSuspend = has_permission(PERMISSIONS.businessSuspend);
  const canReadAudit = has_permission(PERMISSIONS.auditRead);

  const statusMutation = pendingAction === "suspend" ? suspendMutation : activateMutation;

  const onTabChange = (_event: unknown, value: string): void => {
    setTab(value as DetailTab);
  };

  const onEditSubmit = (values: BusinessEditForm): void => {
    patchMutation.mutate(values);
  };

  if (id === undefined) {
    return (
      <Box>
        <Alert severity="error">Geçersiz işletme adresi: kimlik bilgisi eksik.</Alert>
      </Box>
    );
  }

  if (detailQuery.isLoading) return <LoadingState />;
  if (detailQuery.isError) return <ErrorState error={detailQuery.error} />;

  const detail = detailQuery.data!;

  const infoFields: Array<{ label: string; value: string }> = [
    { label: "Şehir (cityId)", value: detail.cityId },
    { label: "Sahip (ownerId)", value: detail.ownerId },
    { label: "Çalışan Sayısı", value: String(detail.employeeCount) },
    { label: "Aktif Hizmet Sayısı", value: String(detail.activeServiceCount) },
    { label: "Son Randevu Sayısı", value: String(detail.recentAppointmentCount) },
    { label: "Kayıt Tarihi", value: formatDateTime(detail.createdAt) },
  ];

  const auditEntries = auditQuery.data?.items ?? [];

  return (
    <Box>
      <PageHeader
        title={detail.name}
        actions={
          <Button variant="outlined" onClick={() => navigate("/businesses")}>
            Listeye Dön
          </Button>
        }
      />
      <Box sx={{ display: "flex", alignItems: "center", gap: 1, flexWrap: "wrap" }}>
        <StatusBadge status={detail.status} />
        {canSuspend && detail.status === "Active" ? (
          <Button color="warning" variant="outlined" onClick={() => setPendingAction("suspend")}>
            Askıya Al
          </Button>
        ) : null}
        {canSuspend && detail.status === "Suspended" ? (
          <Button color="success" variant="outlined" onClick={() => setPendingAction("activate")}>
            Aktifleştir
          </Button>
        ) : null}
      </Box>
      <Tabs value={tab} onChange={onTabChange} sx={{ borderBottom: 1, borderColor: "divider", mb: 2 }}>
        <Tab value="general" label="Genel" />
        {canReadAudit ? <Tab value="audit" label="Audit Geçmişi" /> : null}
      </Tabs>

      {tab === "audit" && canReadAudit ? (
        auditQuery.isLoading ? (
          <LoadingState />
        ) : auditQuery.isError ? (
          <ErrorState error={auditQuery.error} />
        ) : auditEntries.length === 0 ? (
          <EmptyState label="Bu işletme için denetim kaydı bulunamadı." />
        ) : (
          <TableContainer>
            <Table size="small" aria-label="işletme denetim kayıtları">
              <TableHead>
                <TableRow>
                  <TableCell>Tarih</TableCell>
                  <TableCell>Aktör</TableCell>
                  <TableCell>Aksiyon</TableCell>
                  <TableCell>Gerekçe</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {auditEntries.map((entry, index) => (
                  <TableRow key={`${entry.createdAtUtc}-${entry.action}-${index}`}>
                    <TableCell>{formatDateTime(entry.createdAtUtc)}</TableCell>
                    <TableCell>
                      <Typography variant="body2" noWrap title={entry.actorUserId} sx={{ maxWidth: 220 }}>
                        {entry.actorUserId}
                      </Typography>
                      <Typography variant="caption" color="text.secondary" sx={{ display: "block" }}>
                        {entry.actorRole}
                      </Typography>
                    </TableCell>
                    <TableCell>{entry.action}</TableCell>
                    <TableCell>{entry.reason ?? "-"}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        )
      ) : (
        <>
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
                Açıklama
              </Typography>
              <Typography variant="body1">{detail.description}</Typography>
            </Box>
          ) : null}
          {canUpdate ? (
            <Card sx={{ mt: 3 }}>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Bilgileri Düzenle
                </Typography>
                <form onSubmit={handleSubmit(onEditSubmit)} noValidate>
                  <Grid container spacing={2}>
                    <Grid size={{ xs: 12 }}>
                      <Controller
                        control={control}
                        name="name"
                        render={({ field, fieldState }) => (
                          <TextField
                            label="İşletme Adı"
                            fullWidth
                            value={field.value}
                            onChange={field.onChange}
                            onBlur={field.onBlur}
                            error={fieldState.error !== undefined}
                            helperText={fieldState.error?.message ?? "İşletmenin görünen adı."}
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
                            label="Açıklama"
                            fullWidth
                            multiline
                            minRows={3}
                            value={field.value}
                            onChange={field.onChange}
                            onBlur={field.onBlur}
                            error={fieldState.error !== undefined}
                            helperText={
                              fieldState.error?.message ?? "İşletme hakkında kısa açıklama."
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
      )}

      <ReasonConfirmDialog
        open={pendingAction !== null}
        title={pendingAction === "suspend" ? "İşletmeyi Askıya Al" : "İşletmeyi Aktifleştir"}
        impact={pendingAction === "suspend" ? SUSPEND_IMPACT : ACTIVATE_IMPACT}
        confirmLabel={pendingAction === "suspend" ? "Askıya Al" : "Aktifleştir"}
        busy={statusMutation.isPending}
        onCancel={() => setPendingAction(null)}
        onConfirm={(reason) => statusMutation.mutate(reason)}
      />
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
