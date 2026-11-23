import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Box, Card, CardContent, Grid, MenuItem, Stack, TextField, Typography } from "@mui/material";
import { toQueryString } from "../../../shared/api/types";
import { EmptyState, ErrorState, LoadingState, PageHeader } from "../../../shared/components/states";
import { ownerGetItem, useOwnedBusinesses } from "../api/ownerClient";
import type { OwnerDashboardResponse } from "../types";
import { formatMoney, todayIsoDate } from "../types";

/**
 * İşletme sahibi paneli (Modül 15.1 + günlük satış özeti).
 * Kapsam daima seçili işletmedir; tarih değiştirilerek diğer günlerin
 * özeti görüntülenir. Tüm hesaplar SUNUCUDA yapılır (GetBusinessDashboard
 * ve GetDailySalesSummary sorguları); arayüz yalnızca gösterir.
 */

function KpiCard({ label, value }: { label: string; value: string }) {
  return (
    <Grid size={{ xs: 12, sm: 6, md: 4 }}>
      <Card>
        <CardContent>
          <Typography variant="body2" color="text.secondary">
            {label}
          </Typography>
          <Typography variant="h5" component="p" sx={{ mt: 1 }}>
            {value}
          </Typography>
        </CardContent>
      </Card>
    </Grid>
  );
}

export function OwnerDashboardPage() {
  const businessesQuery = useOwnedBusinesses();
  const businesses = businessesQuery.data?.items ?? [];
  const [selectedId, setSelectedId] = useState("");
  const [date, setDate] = useState<string>(todayIsoDate());

  // Tek işletme otomatik seçilir; açık seçim yoksa ilk işletme kullanılır.
  const businessId = selectedId || businesses[0]?.id || "";
  const effectiveDate = date || todayIsoDate();

  const dashboardQuery = useQuery({
    queryKey: ["owner", "dashboard", businessId, effectiveDate],
    queryFn: ({ signal }) =>
      ownerGetItem<OwnerDashboardResponse>(
        `/businesses/${businessId}/dashboard${toQueryString({ date: effectiveDate })}`,
        signal,
      ),
    enabled: businessId !== "",
  });

  if (businessesQuery.isLoading) return <LoadingState />;
  if (businessesQuery.isError) return <ErrorState error={businessesQuery.error} />;
  if (businesses.length === 0) {
    return (
      <Box>
        <PageHeader title="İşletme Paneli" />
        <EmptyState label="Size ait işletme bulunamadı." />
      </Box>
    );
  }

  const dashboard = dashboardQuery.data?.dashboard;
  const summary = dashboardQuery.data?.summary;

  return (
    <Box>
      <PageHeader title="İşletme Paneli" />
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
          type="date"
          label="Gün"
          value={date}
          onChange={(e) => setDate(e.target.value)}
          slotProps={{ inputLabel: { shrink: true } }}
          sx={{ minWidth: 160 }}
        />
      </Stack>

      {dashboardQuery.isLoading ? (
        <LoadingState />
      ) : dashboardQuery.isError ? (
        <ErrorState error={dashboardQuery.error} />
      ) : (
        <>
          <Typography variant="h6" component="h2" sx={{ mb: 1 }}>
            Günlük Randevu Özeti
          </Typography>
          <Grid container spacing={2}>
            <KpiCard label="Bugünkü Randevular" value={String(dashboard?.bugunkiRandevular ?? 0)} />
            <KpiCard label="Tamamlanan" value={String(dashboard?.tamamlanan ?? 0)} />
            <KpiCard label="İptaller" value={String(dashboard?.iptaller ?? 0)} />
            <KpiCard label="Gelmeyen (NoShow)" value={String(dashboard?.noShow ?? 0)} />
            <KpiCard
              label="Doluluk Oranı"
              value={`${Math.round((dashboard?.dolulukOrani ?? 0) * 100)} %`}
            />
          </Grid>

          <Typography variant="h6" component="h2" sx={{ mb: 1, mt: 3 }}>
            Günlük Satış Özeti
          </Typography>
          <Grid container spacing={2}>
            <KpiCard label="Satış Sayısı" value={String(summary?.salesCount ?? 0)} />
            <KpiCard label="Toplam Satış" value={formatMoney(summary?.totalSales ?? 0)} />
            <KpiCard label="Net Nakit Tahsilat" value={formatMoney(summary?.cashCollected ?? 0)} />
            <KpiCard label="Net Kart Tahsilat" value={formatMoney(summary?.cardCollected ?? 0)} />
            <KpiCard label="Gider Toplamı" value={formatMoney(summary?.totalExpenses ?? 0)} />
            <KpiCard
              label="Bekleyen Alacak"
              value={formatMoney(summary?.outstandingReceivables ?? 0)}
            />
            <KpiCard label="Kasa" value={summary?.isCashOpen ? "Açık" : "Kapalı"} />
          </Grid>
        </>
      )}
    </Box>
  );
}
