import { Card, CardContent, Grid, Typography, Box } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { getItem } from "../../shared/api/client";
import { ErrorState, LoadingState, PageHeader } from "../../shared/components/states";

/**
 * Platform dashboard (dokuman 8): KPI'lar API'den gelir; frontend
 * hesaplama yeniden uretmez (dokuman 19.3).
 */
interface PlatformOverview {
  totalBusinesses: number;
  activeBusinesses: number;
  suspendedBusinesses: number;
  newRegistrations30Days: number;
  totalAppointments: number;
  completedAppointments: number;
  cancellationRate: number;
  noShowRate: number;
}

interface SubscriptionRevenue {
  activeSubscriptionCount: number;
  mrr: number;
}

export function DashboardPage() {
  const overview = useQuery({
    queryKey: ["reports", "platform-overview"],
    queryFn: () => getItem<PlatformOverview>("/reports/platform-overview"),
  });

  const revenue = useQuery({
    queryKey: ["reports", "subscription-revenue"],
    queryFn: () => getItem<SubscriptionRevenue>("/reports/subscription-revenue"),
  });

  if (overview.isLoading) return <LoadingState />;
  if (overview.isError) return <ErrorState error={overview.error} />;

  const data = overview.data!;
  const mrr = revenue.data?.mrr;

  const cards: Array<{ label: string; value: string }> = [
    { label: "Toplam Isletme", value: String(data.totalBusinesses) },
    { label: "Aktif Isletme", value: String(data.activeBusinesses) },
    { label: "Askidaki Isletme", value: String(data.suspendedBusinesses) },
    { label: "Yeni Kayit (30 gun)", value: String(data.newRegistrations30Days) },
    { label: "Toplam Randevu", value: String(data.totalAppointments) },
    { label: "Tamamlanan Randevu", value: String(data.completedAppointments) },
    { label: "Iptal Orani", value: `%${(data.cancellationRate ?? 0).toFixed(1)}` },
    { label: "No-Show Orani", value: `%${(data.noShowRate ?? 0).toFixed(1)}` },
    { label: "MRR", value: mrr !== undefined ? `${mrr.toLocaleString("tr-TR")} TL` : "-" },
  ];

  return (
    <Box>
      <PageHeader title="Dashboard" />
      <Grid container spacing={2}>
        {cards.map((card) => (
          <Grid size={{ xs: 12, sm: 6, md: 4, lg: 3 }} key={card.label}>
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
    </Box>
  );
}
