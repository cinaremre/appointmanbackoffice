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
  CampaignListItem,
  CampaignPerformance,
  ReengagementAttempt,
  RecoveredSlot,
  RecoveredSlotReport,
  WaitlistOverview,
} from "../types";
import {
  CAMPAIGN_CHANNEL_LABELS,
  RECOVERED_SLOT_STATUS_LABELS,
  REENGAGEMENT_STATUS_LABELS,
} from "../types";

/**
 * Pazarlama sayfası (doküman Modül 15): kampanyalar, bekleme listesi ve geri
 * çağırma denemeleri. Tüm sayılar API'den gelir; frontend hesaplama üretmez.
 */

/** ISO tarih -> "dd.MM.yyyy HH:mm"; geçersizse ham değer korunur. */
function formatDateTime(value?: string | null): string {
  if (!value) return "-";
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : format(date, "dd.MM.yyyy HH:mm");
}

function formatMoney(value: number): string {
  return `${Number(value ?? 0).toLocaleString("tr-TR")} TL`;
}

/** GUID kısaltması; tablolarda uzun kimlikleri okunur tutar. */
function shortId(value?: string | null): string {
  return value ? value.slice(0, 8) : "-";
}

const campaignColumns: DataTableColumn<CampaignListItem>[] = [
  { key: "name", header: "Kampanya", render: (row) => row.name },
  {
    key: "channel",
    header: "Kanal",
    render: (row) => CAMPAIGN_CHANNEL_LABELS[row.channel] ?? String(row.channel),
  },
  {
    key: "status",
    header: "Durum",
    render: (row) => <StatusBadge status={row.isActive ? "Aktif" : "Pasif"} />,
  },
  { key: "spend", header: "Toplam Harcama", render: (row) => formatMoney(row.totalSpend) },
  {
    key: "attributed",
    header: "Atfedilen Randevu",
    render: (row) => String(row.attributedAppointments),
  },
];

const recoveredSlotColumns: DataTableColumn<RecoveredSlot>[] = [
  {
    key: "cancelled",
    header: "İptal Edilen Randevu",
    render: (row) => shortId(row.cancelledAppointmentId),
  },
  {
    key: "status",
    header: "Durum",
    render: (row) => (
      <StatusBadge status={RECOVERED_SLOT_STATUS_LABELS[row.status] ?? String(row.status)} />
    ),
  },
  { key: "released", header: "Açığa Çıkış", render: (row) => formatDateTime(row.releasedAt) },
  { key: "rebooked", header: "Yeniden Dolum", render: (row) => formatDateTime(row.rebookedAt) },
];

const attemptColumns: DataTableColumn<ReengagementAttempt>[] = [
  { key: "customer", header: "Müşteri", render: (row) => shortId(row.customerId) },
  { key: "business", header: "İşletme", render: (row) => shortId(row.businessId) },
  {
    key: "status",
    header: "Durum",
    render: (row) => (
      <StatusBadge status={REENGAGEMENT_STATUS_LABELS[row.status] ?? String(row.status)} />
    ),
  },
  { key: "triggered", header: "Tetiklenme", render: (row) => formatDateTime(row.triggeredAt) },
  {
    key: "converted",
    header: "Dönüşen Randevu",
    render: (row) => shortId(row.convertedAppointmentId),
  },
];

export function MarketingPage() {
  const [tab, setTab] = useState(0);
  const theme = useTheme();
  const is_mobile = useMediaQuery(theme.breakpoints.down("sm"));

  // Ortak işletme filtresi; boşken platform geneli raporlar.
  const [businessId, setBusinessId] = useState("");

  // Kampanyalar filtreleri ve sayfalama.
  const [channel, setChannel] = useState("");
  const [isActive, setIsActive] = useState("");
  const [campaignPage, setCampaignPage] = useState(1);
  const [campaignPageSize, setCampaignPageSize] = useState(50);

  // Performans dialog'u hangi kampanya için açık.
  const [performanceCampaignId, setPerformanceCampaignId] = useState<string | null>(null);

  // Kurtarılan kapasite tarih aralığı.
  const [slotFrom, setSlotFrom] = useState("");
  const [slotTo, setSlotTo] = useState("");

  // Geri çağırma durum filtresi.
  const [attemptStatus, setAttemptStatus] = useState("");

  const campaigns = useQuery({
    queryKey: [
      "marketing",
      "campaigns",
      { businessId, channel, isActive, page: campaignPage, pageSize: campaignPageSize },
    ],
    queryFn: ({ signal }) =>
      getList<CampaignListItem>(
        `/marketing/campaigns${toQueryString({
          businessId,
          channel,
          isActive,
          page: campaignPage,
          pageSize: campaignPageSize,
        })}`,
        signal,
      ),
    enabled: tab === 0,
  });

  const performance = useQuery({
    queryKey: ["marketing", "campaign-performance", performanceCampaignId],
    queryFn: ({ signal }) =>
      getItem<CampaignPerformance>(
        `/marketing/campaigns/${performanceCampaignId}/performance`,
        signal,
      ),
    enabled: performanceCampaignId !== null,
  });

  const waitlist = useQuery({
    queryKey: ["marketing", "waitlist-overview", { businessId }],
    queryFn: ({ signal }) =>
      getItem<WaitlistOverview>(`/waitlist/overview${toQueryString({ businessId })}`, signal),
    enabled: tab === 1,
  });

  const recovered = useQuery({
    queryKey: ["marketing", "recovered-slots", { businessId, fromDate: slotFrom, toDate: slotTo }],
    queryFn: ({ signal }) =>
      getItem<RecoveredSlotReport>(
        `/waitlist/recovered-slots${toQueryString({
          businessId,
          fromDate: slotFrom,
          toDate: slotTo,
        })}`,
        signal,
      ),
    enabled: tab === 1,
  });

  const attempts = useQuery({
    queryKey: ["marketing", "reengagement-attempts", { businessId, status: attemptStatus }],
    queryFn: ({ signal }) =>
      getList<ReengagementAttempt>(
        `/reengagement/attempts${toQueryString({ businessId, status: attemptStatus })}`,
        signal,
      ),
    enabled: tab === 2,
  });

  const overviewCards = waitlist.data
    ? [
        { label: "Aktif Kayıt", value: String(waitlist.data.activeEntries) },
        { label: "Bekleyen Teklif", value: String(waitlist.data.pendingOffers) },
        { label: "Kabul Edilen Teklif", value: String(waitlist.data.acceptedOffers) },
        { label: "Süresi Dolan Teklif", value: String(waitlist.data.expiredOffers) },
        { label: "Yeniden Doldurulan Slot", value: String(waitlist.data.rebookedSlots) },
        { label: "Kaybedilen Slot", value: String(waitlist.data.lostSlots) },
      ]
    : [];

  return (
    <Box>
      <PageHeader title="Pazarlama" />

      <TextField
        label="İşletme ID (UUID, opsiyonel)"
        value={businessId}
        onChange={(e) => {
          setBusinessId(e.target.value);
          setCampaignPage(1);
        }}
        size="small"
        sx={{ minWidth: { xs: "100%", sm: 280 }, mb: 2 }}
      />

      <Tabs value={tab} onChange={(_, value: number) => setTab(value)} aria-label="pazarlama sekmeleri">
        <Tab label="Kampanyalar" />
        <Tab label="Bekleme Listesi" />
        <Tab label="Geri Çağırma" />
      </Tabs>

      <Box sx={{ mt: 2 }}>
        {tab === 0 ? (
          <Box>
            <Stack direction="row" spacing={2} useFlexGap sx={{ mb: 2, flexWrap: "wrap" }}>
              <TextField
                select
                label="Kanal"
                value={channel}
                onChange={(e) => {
                  setChannel(e.target.value);
                  setCampaignPage(1);
                }}
                size="small"
                sx={{ minWidth: { xs: "100%", sm: 180 } }}
              >
                <MenuItem value="">Tümü</MenuItem>
                {Object.entries(CAMPAIGN_CHANNEL_LABELS).map(([value, label]) => (
                  <MenuItem key={value} value={value}>
                    {label}
                  </MenuItem>
                ))}
              </TextField>
              <TextField
                select
                label="Aktiflik"
                value={isActive}
                onChange={(e) => {
                  setIsActive(e.target.value);
                  setCampaignPage(1);
                }}
                size="small"
                sx={{ minWidth: { xs: "100%", sm: 160 } }}
              >
                <MenuItem value="">Tümü</MenuItem>
                <MenuItem value="true">Aktif</MenuItem>
                <MenuItem value="false">Pasif</MenuItem>
              </TextField>
            </Stack>

            <DataTable<CampaignListItem>
              columns={campaignColumns}
              rows={campaigns.data?.items}
              loading={campaigns.isLoading}
              error={campaigns.error}
              page={campaignPage}
              pageSize={campaignPageSize}
              totalCount={campaigns.data?.pagination.totalCount ?? 0}
              onPageChange={setCampaignPage}
              onPageSizeChange={setCampaignPageSize}
              emptyLabel="Kampanya bulunamadı."
              ariaLabel="kampanya listesi"
              onRefresh={() => campaigns.refetch()}
              rowActions={(row) => (
                <Button
                  size="small"
                  onClick={() => setPerformanceCampaignId(row.id)}
                  aria-label={`${row.name} performansı`}
                >
                  Performans
                </Button>
              )}
            />
          </Box>
        ) : null}

        {tab === 1 ? (
          <Box>
            {waitlist.isLoading ? (
              <LoadingState />
            ) : waitlist.isError ? (
              <ErrorState error={waitlist.error} />
            ) : (
              <Grid container spacing={2}>
                {overviewCards.map((card) => (
                  <Grid size={{ xs: 12, sm: 6, md: 4 }} key={card.label}>
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

            <Typography variant="h6" component="h2" sx={{ mt: 4, mb: 2 }}>
              Kurtarılan Kapasite
            </Typography>
            <Stack direction="row" spacing={2} useFlexGap sx={{ mb: 2, flexWrap: "wrap" }}>
              <TextField
                label="Başlangıç"
                type="date"
                value={slotFrom}
                onChange={(e) => setSlotFrom(e.target.value)}
                size="small"
                slotProps={{ inputLabel: { shrink: true } }}
                sx={{ minWidth: { xs: "100%", sm: 170 } }}
              />
              <TextField
                label="Bitiş"
                type="date"
                value={slotTo}
                onChange={(e) => setSlotTo(e.target.value)}
                size="small"
                slotProps={{ inputLabel: { shrink: true } }}
                sx={{ minWidth: { xs: "100%", sm: 170 } }}
              />
              {recovered.data ? (
                <Typography variant="body2" color="text.secondary" sx={{ alignSelf: "center" }}>
                  Toplam yeniden dolum: {recovered.data.totalRebooked}
                </Typography>
              ) : null}
            </Stack>

            {recovered.isLoading ? (
              <LoadingState />
            ) : recovered.isError ? (
              <ErrorState error={recovered.error} />
            ) : recovered.data && recovered.data.items.length > 0 ? (
              <DataTable<RecoveredSlot>
                columns={recoveredSlotColumns}
                rows={recovered.data.items}
                loading={false}
                error={null}
                page={1}
                pageSize={50}
                totalCount={recovered.data.items.length}
                onPageChange={() => {}}
                onPageSizeChange={() => {}}
                ariaLabel="kurtarılan slotlar"
                onRefresh={() => recovered.refetch()}
              />
            ) : (
              <EmptyState label="Secili aralıkta kurtarılan slot bulunamadı." />
            )}
          </Box>
        ) : null}

        {tab === 2 ? (
          <Box>
            <Stack direction="row" spacing={2} useFlexGap sx={{ mb: 2, flexWrap: "wrap" }}>
              <TextField
                select
                label="Durum"
                value={attemptStatus}
                onChange={(e) => setAttemptStatus(e.target.value)}
                size="small"
                sx={{ minWidth: { xs: "100%", sm: 200 } }}
              >
                <MenuItem value="">Tümü</MenuItem>
                {Object.entries(REENGAGEMENT_STATUS_LABELS).map(([value, label]) => (
                  <MenuItem key={value} value={value}>
                    {label}
                  </MenuItem>
                ))}
              </TextField>
            </Stack>

            <DataTable<ReengagementAttempt>
              columns={attemptColumns}
              rows={attempts.data?.items}
              loading={attempts.isLoading}
              error={attempts.error}
              page={1}
              pageSize={50}
              totalCount={attempts.data?.items.length ?? 0}
              onPageChange={() => {}}
              onPageSizeChange={() => {}}
              emptyLabel="Geri çağırma denemesi bulunamadı."
              ariaLabel="geri çağırma denemeleri"
              onRefresh={() => attempts.refetch()}
            />
          </Box>
        ) : null}
      </Box>

      {/* Kampanya performans dialog'u: harcama ve atfedilen sayılar. */}
      <Dialog
        open={performanceCampaignId !== null}
        onClose={() => setPerformanceCampaignId(null)}
        fullWidth
        maxWidth="sm"
        fullScreen={is_mobile}
      >
        <DialogTitle>Kampanya Performansı</DialogTitle>
        <DialogContent>
          {performance.isLoading ? (
            <LoadingState />
          ) : performance.isError ? (
            <ErrorState error={performance.error} />
          ) : performance.data ? (
            <Stack spacing={1} sx={{ pt: 1 }}>
              <Typography variant="body1">
                <strong>{performance.data.campaignName}</strong>
              </Typography>
              <Typography variant="body2">
                Toplam Harcama: {formatMoney(performance.data.totalSpend)}
              </Typography>
              <Typography variant="body2">
                Atfedilen Randevu: {performance.data.attributedAppointments}
              </Typography>
              <Typography variant="body2">
                Atfedilen Tamamlanan Randevu: {performance.data.attributedCompleted}
              </Typography>
              {performance.data.roasDesc ? (
                <Typography variant="body2" color="text.secondary">
                  {performance.data.roasDesc}
                </Typography>
              ) : null}
            </Stack>
          ) : null}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setPerformanceCampaignId(null)}>Kapat</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
