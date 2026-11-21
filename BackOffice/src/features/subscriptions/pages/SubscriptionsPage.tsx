import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Checkbox,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  FormControlLabel,
  MenuItem,
  Snackbar,
  Stack,
  Switch,
  TextField,
  Typography,
} from "@mui/material";
import { Controller, useForm } from "react-hook-form";
import { useState } from "react";
import { z } from "zod";
import { useAuth } from "../../../app/providers/AuthContext";
import { ApiError, getItem, getList, postItem } from "../../../shared/api/client";
import { DataTable } from "../../../shared/components/DataTable";
import type { DataTableColumn } from "../../../shared/components/DataTable";
import { ErrorState, LoadingState, PageHeader } from "../../../shared/components/states";
import { StatusBadge } from "../../../shared/components/StatusBadge";
import { PERMISSIONS } from "../../../shared/constants/permissions";
import type {
  BusinessSubscriptionDto,
  FeatureDefinitionDto,
  FeatureOverrideDto,
  SubscriptionPlanDto,
} from "../types";
import {
  BILLING_PERIOD_LABELS,
  BILLING_PERIOD_VALUES,
  formatPrice,
  isGuid,
  quotaLabel,
  subscriptionStatusLabel,
  toBillingPeriod,
} from "../types";

/**
 * Abonelik ve ozellik yonetimi (dokuman Modul 14-15).
 * - Plan listesi: GET /subscription-plans (DataTable).
 * - Plan olusturma: POST /subscription-plans (react-hook-form + zod).
 * - Isletme bazli erisim: aktif abonelik + override listesi + yeni override
 *   formu (POST /businesses/{id}/features/feature-overrides). Override kritik
 *   islemdir; gerekce alani formda ZORUNLUDUR ve denetim kaydina yazilir.
 * Mutasyon sonrasi ["subscriptions"] anahtari topluca gecersiz kilinir.
 */

/** Plan olusturma formu semasi. Fiyat string girilir, gonderimde sayiya cevrilir. */
const planSchema = z.object({
  name: z.string().trim().min(3, "Plan adi en az 3 karakter olmali."),
  price: z
    .string()
    .trim()
    .min(1, "Fiyat zorunludur.")
    .refine((value) => Number(value.replace(",", ".")) > 0, "Fiyat sifirdan buyuk pozitif bir deger olmali."),
  billingPeriod: z.enum(["Monthly", "Yearly"]),
  featureIds: z.array(z.string()),
});
type PlanFormValues = z.infer<typeof planSchema>;

/** Ozellik override formu; gerekce ZORUNLU (kritik islem, dokuman 5.4/24). */
const overrideSchema = z
  .object({
    featureId: z.string().min(1, "Bir ozellik secin."),
    isEnabled: z.boolean(),
    quota: z.string().trim(),
    validFrom: z.string().min(1, "Baslangic tarihi zorunludur."),
    validUntil: z.string().trim(),
    reason: z.string().trim().min(3, "Gerekce zorunludur; en az 3 karakter girin."),
  })
  .refine((v) => v.quota === "" || (Number.isInteger(Number(v.quota)) && Number(v.quota) >= 0), {
    message: "Kota sifir veya daha buyuk tam sayi olmali.",
    path: ["quota"],
  })
  .refine((v) => v.validUntil === "" || v.validUntil >= v.validFrom, {
    message: "Bitis tarihi baslangictan once olamaz.",
    path: ["validUntil"],
  });
type OverrideFormValues = z.infer<typeof overrideSchema>;

function todayIsoDate(): string {
  return new Date().toISOString().slice(0, 10);
}

export function SubscriptionsPage() {
  const { has_permission } = useAuth();
  const queryClient = useQueryClient();
  const canUpdate = has_permission(PERMISSIONS.subscriptionUpdate);
  const canOverride = has_permission(PERMISSIONS.featureOverride);

  const [snack, setSnack] = useState<{ severity: "success" | "error"; message: string } | null>(null);
  const notify = (message: string, severity: "success" | "error" = "success") =>
    setSnack({ message, severity });

  // === Okuma sorgulari (anahtarlar modul onekiyle) ===
  const plansQuery = useQuery({
    queryKey: ["subscriptions", "plans"],
    queryFn: async ({ signal }) => {
      const { items } = await getList<SubscriptionPlanDto>("/subscription-plans", signal);
      // Enum sayisal da gelebilir; toleransli etikete cevir.
      return items.map((plan) => ({ ...plan, billingPeriod: toBillingPeriod(plan.billingPeriod) }));
    },
  });

  const featuresQuery = useQuery({
    queryKey: ["subscriptions", "feature-definitions"],
    queryFn: ({ signal }) => getList<FeatureDefinitionDto>("/features", signal),
  });

  // === Isletme bazli erisim bolumu ===
  const [businessInput, setBusinessInput] = useState("");
  const [businessError, setBusinessError] = useState<string | null>(null);
  const [businessId, setBusinessId] = useState<string | null>(null);

  const loadBusiness = () => {
    const value = businessInput.trim();
    if (value === "") {
      setBusinessError(null);
      setBusinessId(null);
      return;
    }
    if (!isGuid(value)) {
      setBusinessError("Gecerli bir isletme kimligi (GUID) girin.");
      return;
    }
    setBusinessError(null);
    setBusinessId(value);
  };

  // Aktif abonelik yoksa endpoint basariyla data null doner; hata degil.
  const subscriptionQuery = useQuery({
    queryKey: ["subscriptions", "business-subscription", businessId],
    queryFn: ({ signal }) =>
      getItem<BusinessSubscriptionDto | null>(`/businesses/${businessId}/subscription`, signal),
    enabled: businessId !== null,
  });

  const overridesQuery = useQuery({
    queryKey: ["subscriptions", "feature-overrides", businessId],
    queryFn: ({ signal }) => getList<FeatureOverrideDto>(`/businesses/${businessId}/features`, signal),
    enabled: businessId !== null,
  });

  const featureNameByCode = new Map(
    (featuresQuery.data?.items ?? []).map((feature) => [feature.code, feature.name]),
  );

  // Backend tum gecmisi dondurur; arayuzu gecerlilik penceresi icindekiler.
  // Anlik zaman mount aninda bir kez hesaplanir (render safi kalir).
  const [loadedAtMs] = useState(() => Date.now());
  const activeOverrides = (overridesQuery.data?.items ?? []).filter((row) => {
    const from = Date.parse(row.validFrom);
    const until = row.validUntil ? Date.parse(row.validUntil) : Number.POSITIVE_INFINITY;
    return from <= loadedAtMs && loadedAtMs <= until;
  });

  // === Plan olusturma mutasyonu ===
  const [planOpen, setPlanOpen] = useState(false);
  const [quotas, setQuotas] = useState<Record<string, string>>({});

  const {
    register: registerPlan,
    handleSubmit: handleSubmitPlan,
    reset: resetPlan,
    control: planControl,
    formState: { errors: planErrors },
  } = useForm<PlanFormValues>({
    resolver: zodResolver(planSchema),
    defaultValues: { name: "", price: "", billingPeriod: "Monthly", featureIds: [] },
  });

  const createPlanMutation = useMutation({
    mutationFn: (values: PlanFormValues) =>
      postItem<string>("/subscription-plans", {
        name: values.name.trim(),
        price: Number(values.price.replace(",", ".")),
        currency: "TRY",
        // Backend enum'u sayisal bekler: 0=Monthly, 1=Yearly.
        billingPeriod: BILLING_PERIOD_VALUES[values.billingPeriod],
        features: values.featureIds.map((featureId) => ({
          featureId,
          isEnabled: true,
          includedQuota: quotas[featureId] ? Number(quotas[featureId].replace(",", ".")) : null,
        })),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["subscriptions"] });
      notify("Abonelik plani olusturuldu.");
      closePlanDialog();
    },
    onError: (error) =>
      notify(error instanceof ApiError ? error.userMessage : "Plan olusturulamadi.", "error"),
  });

  function openPlanDialog() {
    resetPlan({ name: "", price: "", billingPeriod: "Monthly", featureIds: [] });
    setQuotas({});
    setPlanOpen(true);
  }

  function closePlanDialog() {
    setPlanOpen(false);
    resetPlan({ name: "", price: "", billingPeriod: "Monthly", featureIds: [] });
    setQuotas({});
  }

  // === Ozellik override mutasyonu ===
  const {
    register: registerOverride,
    handleSubmit: handleSubmitOverride,
    reset: resetOverride,
    control: overrideControl,
    formState: { errors: overrideErrors },
  } = useForm<OverrideFormValues>({
    resolver: zodResolver(overrideSchema),
    defaultValues: {
      featureId: "",
      isEnabled: true,
      quota: "",
      validFrom: todayIsoDate(),
      validUntil: "",
      reason: "",
    },
  });

  const grantOverrideMutation = useMutation({
    mutationFn: (values: OverrideFormValues) => {
      if (businessId === null) throw new Error("Once bir isletme getirin.");
      return postItem<string>(`/businesses/${businessId}/features/feature-overrides`, {
        featureId: values.featureId,
        isEnabled: values.isEnabled,
        quotaOverride: values.quota === "" ? null : Number(values.quota),
        validFrom: new Date(`${values.validFrom}T00:00:00Z`).toISOString(),
        validUntil: values.validUntil === "" ? null : new Date(`${values.validUntil}T00:00:00Z`).toISOString(),
        reason: values.reason,
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["subscriptions"] });
      notify("Ozellik override kaydi olusturuldu.");
      resetOverride({
        featureId: "",
        isEnabled: true,
        quota: "",
        validFrom: todayIsoDate(),
        validUntil: "",
        reason: "",
      });
    },
    onError: (error) =>
      notify(error instanceof ApiError ? error.userMessage : "Override kaydi olusturulamadi.", "error"),
  });

  // === Plan tablosu sutunlari ===
  const planColumns: DataTableColumn<SubscriptionPlanDto>[] = [
    { key: "name", header: "Plan Adi", render: (row) => row.name },
    { key: "price", header: "Fiyat", render: (row) => formatPrice(row.price, row.currency) },
    { key: "period", header: "Donem", render: (row) => BILLING_PERIOD_LABELS[row.billingPeriod] },
    {
      key: "isActive",
      header: "Aktif",
      render: (row) => <StatusBadge status={row.isActive ? "Aktif" : "Pasif"} />,
    },
    { key: "featureCount", header: "Ozellik Sayisi", render: (row) => String(row.features.length) },
  ];

  const overrideColumns: DataTableColumn<FeatureOverrideDto>[] = [
    {
      key: "feature",
      header: "Ozellik",
      render: (row) =>
        featureNameByCode.get(row.featureCode)
          ? `${featureNameByCode.get(row.featureCode)} (${row.featureCode})`
          : row.featureCode,
    },
    {
      key: "state",
      header: "Durum",
      render: (row) => <StatusBadge status={row.isEnabled ? "Aktif" : "Pasif"} />,
    },
    {
      key: "quota",
      header: "Kota",
      render: (row) => (row.quotaOverride === null || row.quotaOverride === undefined ? "Plan kotasi" : String(row.quotaOverride)),
    },
    {
      key: "validFrom",
      header: "Baslangic",
      render: (row) => new Date(row.validFrom).toLocaleDateString("tr-TR"),
    },
    {
      key: "validUntil",
      header: "Bitis",
      render: (row) => (row.validUntil ? new Date(row.validUntil).toLocaleDateString("tr-TR") : "Belirsiz"),
    },
    { key: "reason", header: "Gerekce", render: (row) => row.reason ?? "-" },
  ];

  const plans = plansQuery.data;
  const subscription = subscriptionQuery.data ?? null;

  return (
    <Box>
      <PageHeader
        title="Abonelikler"
        actions={
          canUpdate ? (
            <Button variant="contained" onClick={openPlanDialog}>
              Plan Olustur
            </Button>
          ) : undefined
        }
      />

      <DataTable<SubscriptionPlanDto>
        ariaLabel="abonelik planlari listesi"
        columns={planColumns}
        rows={plans}
        loading={plansQuery.isPending}
        error={plansQuery.error}
        page={1}
        pageSize={50}
        totalCount={plans?.length ?? 0}
        onPageChange={() => {}}
        onPageSizeChange={() => {}}
        emptyLabel="Henuz abonelik plani tanimlanmamis."
        onRefresh={() => plansQuery.refetch()}
      />

      <Divider sx={{ my: 4 }} />

      <Box>
        <Typography variant="h6" component="h2" gutterBottom>
          Isletme Bazli Ozellik Erisimi
        </Typography>
        <Stack direction="row" spacing={2} sx={{ alignItems: "flex-start", flexWrap: "wrap" }}>
          <TextField
            label="Isletme Kimligi (GUID)"
            size="small"
            sx={{ minWidth: 320 }}
            value={businessInput}
            onChange={(e) => setBusinessInput(e.target.value)}
            onBlur={loadBusiness}
            onKeyDown={(e) => {
              if (e.key === "Enter") loadBusiness();
            }}
            error={businessError !== null}
            helperText={businessError ?? "Abonelik ve override kayitlarini getirmek icin GUID girip Enter'a basin."}
          />
          <Button variant="outlined" onClick={loadBusiness} sx={{ height: 40 }}>
            Getir
          </Button>
        </Stack>

        {businessId !== null ? (
          <Box sx={{ mt: 3 }}>
            {subscriptionQuery.isPending ? (
              <LoadingState />
            ) : subscriptionQuery.isError ? (
              <ErrorState error={subscriptionQuery.error} />
            ) : subscription ? (
              <Card variant="outlined" sx={{ mb: 2 }}>
                <CardContent>
                  <Stack direction="row" spacing={2} sx={{ alignItems: "center", flexWrap: "wrap" }}>
                    <Typography variant="subtitle1">Aktif Abonelik: {subscription.planName}</Typography>
                    <StatusBadge status={subscriptionStatusLabel(subscription.status)} />
                    <Typography variant="body2" color="text.secondary">
                      Baslangic: {new Date(subscription.startAt).toLocaleDateString("tr-TR")}
                      {subscription.endAt
                        ? ` - Bitis: ${new Date(subscription.endAt).toLocaleDateString("tr-TR")}`
                        : " - Belirsiz sure"}
                    </Typography>
                  </Stack>
                  <Stack direction="row" spacing={1} sx={{ mt: 1, flexWrap: "wrap" }}>
                    {subscription.planFeatures.map((feature) => (
                      <Chip
                        key={feature.featureCode}
                        size="small"
                        variant="outlined"
                        label={`${feature.featureCode}${feature.isEnabled ? "" : " (kapali)"} - ${quotaLabel(feature.includedQuota)}`}
                      />
                    ))}
                  </Stack>
                </CardContent>
              </Card>
            ) : (
              <Alert severity="info" sx={{ mb: 2 }}>
                Bu isletmenin gecerli aboneligi bulunamadi.
              </Alert>
            )}

            <Typography variant="subtitle1" component="h3" gutterBottom>
              Gecerli Ozellik Override Kayitlari
            </Typography>
            <DataTable<FeatureOverrideDto>
              ariaLabel="ozellik override listesi"
              columns={overrideColumns}
              rows={activeOverrides}
              loading={overridesQuery.isPending}
              error={overridesQuery.error}
              page={1}
              pageSize={50}
              totalCount={activeOverrides.length}
              onPageChange={() => {}}
              onPageSizeChange={() => {}}
              emptyLabel="Bu isletme icin gecerli override kaydi yok."
              onRefresh={() => overridesQuery.refetch()}
            />

            {canOverride ? (
              <Card variant="outlined" sx={{ mt: 3 }}>
                <CardContent>
                  <Typography variant="subtitle1" component="h3" gutterBottom>
                    Yeni Ozellik Override
                  </Typography>
                  <Alert severity="warning" sx={{ mb: 2 }}>
                    Kritik islem: erisim karari denetim kaydina yazilir. Gerekce alani zorunludur.
                  </Alert>
                  <Stack spacing={2}>
                    <Controller
                      name="featureId"
                      control={overrideControl}
                      render={({ field }) => (
                        <TextField
                          select
                          label="Ozellik"
                          {...field}
                          error={overrideErrors.featureId !== undefined}
                          helperText={overrideErrors.featureId?.message ?? "Erisim karari verilecek ozellik."}
                        >
                          {(featuresQuery.data?.items ?? []).map((feature) => (
                            <MenuItem key={feature.id} value={feature.id}>
                              {feature.name} ({feature.code})
                            </MenuItem>
                          ))}
                        </TextField>
                      )}
                    />
                    <Controller
                      name="isEnabled"
                      control={overrideControl}
                      render={({ field }) => (
                        <FormControlLabel
                          control={
                            <Switch
                              checked={field.value}
                              onChange={(e) => field.onChange(e.target.checked)}
                              onBlur={field.onBlur}
                            />
                          }
                          label={field.value ? "Erisim acik" : "Erisim kapali"}
                        />
                      )}
                    />
                    <Stack direction="row" spacing={2} sx={{ flexWrap: "wrap" }}>
                      <TextField
                        label="Kota Override (ops.)"
                        sx={{ minWidth: 180 }}
                        {...registerOverride("quota")}
                        error={overrideErrors.quota !== undefined}
                        helperText={overrideErrors.quota?.message ?? "Bos birakilirsa plan kotasi gecerli kalir."}
                      />
                      <TextField
                        label="Baslangic"
                        type="date"
                        sx={{ minWidth: 180 }}
                        slotProps={{ inputLabel: { shrink: true } }}
                        {...registerOverride("validFrom")}
                        error={overrideErrors.validFrom !== undefined}
                        helperText={overrideErrors.validFrom?.message ?? " "}
                      />
                      <TextField
                        label="Bitis (ops.)"
                        type="date"
                        sx={{ minWidth: 180 }}
                        slotProps={{ inputLabel: { shrink: true } }}
                        {...registerOverride("validUntil")}
                        error={overrideErrors.validUntil !== undefined}
                        helperText={overrideErrors.validUntil?.message ?? "Bos = belirsiz sure."}
                      />
                    </Stack>
                    <TextField
                      label="Gerekce (zorunlu)"
                      multiline
                      minRows={2}
                      {...registerOverride("reason")}
                      error={overrideErrors.reason !== undefined}
                      helperText={overrideErrors.reason?.message ?? "Bu karar denetim kaydina yazilir."}
                    />
                    <Box>
                      <Button
                        variant="contained"
                        color="primary"
                        onClick={handleSubmitOverride((values) => grantOverrideMutation.mutate(values))}
                        disabled={grantOverrideMutation.isPending}
                      >
                        {grantOverrideMutation.isPending ? "Veriliyor..." : "Override Ver"}
                      </Button>
                    </Box>
                  </Stack>
                </CardContent>
              </Card>
            ) : null}
          </Box>
        ) : null}
      </Box>

      {/* Plan olusturma diyalogu */}
      <Dialog open={planOpen} onClose={planOpen ? closePlanDialog : undefined} maxWidth="sm" fullWidth>
        <DialogTitle>Yeni Abonelik Plani</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            <TextField
              label="Plan Adi"
              {...registerPlan("name")}
              error={planErrors.name !== undefined}
              helperText={planErrors.name?.message ?? " "}
            />
            <TextField
              label="Fiyat (TL)"
              {...registerPlan("price")}
              error={planErrors.price !== undefined}
              helperText={planErrors.price?.message ?? "Aylik/yillik plan fiyati."}
            />
            <Controller
              name="billingPeriod"
              control={planControl}
              render={({ field }) => (
                <TextField
                  select
                  label="Faturalandirma Donemi"
                  {...field}
                  error={planErrors.billingPeriod !== undefined}
                  helperText={planErrors.billingPeriod?.message ?? " "}
                >
                  <MenuItem value="Monthly">Aylik</MenuItem>
                  <MenuItem value="Yearly">Yillik</MenuItem>
                </TextField>
              )}
            />
            <Box>
              <Typography variant="subtitle2" gutterBottom>
                Plana Dahil Ozellikler
              </Typography>
              <Controller
                name="featureIds"
                control={planControl}
                render={({ field }) => (
                  <Stack spacing={0.5}>
                    {(featuresQuery.data?.items ?? []).map((feature) => {
                      const checked = field.value.includes(feature.id);
                      return (
                        <Stack key={feature.id} direction="row" spacing={1} sx={{ alignItems: "center" }}>
                          <FormControlLabel
                            sx={{ flexGrow: 1, m: 0 }}
                            control={
                              <Checkbox
                                checked={checked}
                                onChange={(e) =>
                                  field.onChange(
                                    e.target.checked
                                      ? [...field.value, feature.id]
                                      : field.value.filter((id) => id !== feature.id),
                                  )
                                }
                              />
                            }
                            label={feature.name}
                          />
                          <Chip size="small" variant="outlined" label={feature.code} />
                          <TextField
                            size="small"
                            label="Kota (ops.)"
                            sx={{ width: 150 }}
                            disabled={!checked}
                            value={quotas[feature.id] ?? ""}
                            onChange={(e) =>
                              setQuotas((prev) => ({ ...prev, [feature.id]: e.target.value }))
                            }
                            helperText={checked ? "Bos = sinirsiz" : " "}
                          />
                        </Stack>
                      );
                    })}
                    {(featuresQuery.data?.items ?? []).length === 0 ? (
                      <Typography color="text.secondary">
                        Ozellik tanimi bulunamadi; once ozellik katalogu tanimlanmali.
                      </Typography>
                    ) : null}
                  </Stack>
                )}
              />
            </Box>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={closePlanDialog} disabled={createPlanMutation.isPending}>
            Vazgec
          </Button>
          <Button
            variant="contained"
            onClick={handleSubmitPlan((values) => createPlanMutation.mutate(values))}
            disabled={createPlanMutation.isPending}
          >
            {createPlanMutation.isPending ? "Olusturuluyor..." : "Plani Olustur"}
          </Button>
        </DialogActions>
      </Dialog>

      <Snackbar
        open={snack !== null}
        autoHideDuration={4000}
        onClose={() => setSnack(null)}
        anchorOrigin={{ vertical: "bottom", horizontal: "center" }}
      >
        <Alert severity={snack?.severity} variant="filled" onClose={() => setSnack(null)}>
          {snack?.message}
        </Alert>
      </Snackbar>
    </Box>
  );
}
