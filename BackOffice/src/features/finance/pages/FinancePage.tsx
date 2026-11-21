import {
  Alert,
  Box,
  Divider,
  MenuItem,
  Snackbar,
  Stack,
  Tab,
  Tabs,
  TextField,
  Typography,
} from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import type { ReactElement, SyntheticEvent } from "react";
import { useSearchParams } from "react-router-dom";
import { ApiError, getItem, getList } from "../../../shared/api/client";
import { toQueryString } from "../../../shared/api/types";
import { DataTable } from "../../../shared/components/DataTable";
import type { DataTableColumn } from "../../../shared/components/DataTable";
import { PageHeader } from "../../../shared/components/states";
import { StatusBadge } from "../../../shared/components/StatusBadge";
import type {
  AdminSalesListDto,
  CashSessionRow,
  CommissionRow,
  ExpenseRow,
  PaymentRow,
  SaleRow,
} from "../types";
import {
  CASH_SESSION_STATUS_LABELS,
  COMMISSION_STATUS_LABELS,
  PAYMENT_METHOD_LABELS,
  SALE_STATUS_LABELS,
  formatDateTime,
  formatMoney,
  formatSignedMoney,
  isGuid,
  labelFor,
} from "../types";

/**
 * Finans sayfasi (dokuman Modul 16-17). Tamami salt okunur: tutar
 * duzeltmeleri (iade, ters kayit, prim ters cevirme) domain komutlariyla
 * yapilir, arayuzden veri degistirilmez.
 *
 * Sekmeler: Satislar / Tahsilatlar / Iadeler / Kasa / Giderler / Komisyonlar.
 * - Satis ucu OkEnvelope ile "liste + toplam" dondugunden getItem ile
 *   cozumlenir; digerleri OkList (getList) seklindedir.
 * - Iadeler, odeme ucunun isRefund=true filtresiyle ayri sekmede listelenir.
 * - Filtreler (sekme, isletme, tarih, durum) URL query ile senkronize; boylece
 *   link kopyalanabilir ve geri donuste kosul korunur.
 * - Kasa farklari isaretli metinle gosterilir: pozitif yesil "+", negatif
 *   kirmizi "-"; renk tek basina gosterge degildir (dokuman 5.2).
 */

const TABS = [
  { key: "sales", label: "Satislar" },
  { key: "payments", label: "Tahsilatlar" },
  { key: "refunds", label: "Iadeler" },
  { key: "cash", label: "Kasa" },
  { key: "expenses", label: "Giderler" },
  { key: "commissions", label: "Komisyonlar" },
] as const;

type FinanceTab = (typeof TABS)[number]["key"];

/** Tarih araligi filtresi sunan sekmeler. */
const DATE_FILTER_TABS: readonly FinanceTab[] = ["sales", "payments", "refunds", "expenses"];

const CASH_STATUS_OPTIONS = [
  { value: "0", label: "Acik" },
  { value: "1", label: "Kapali" },
];

const COMMISSION_STATUS_OPTIONS = [
  { value: "0", label: "Tahakkuk" },
  { value: "1", label: "Odenmis" },
  { value: "2", label: "Ters Cevrildi" },
];

/** GUID'leri kisa gosterimle listeler; tam deger title icinde tasir. */
function GuidText({ value }: { value: string }) {
  return (
    <Typography variant="body2" component="span" title={value}>
      {value.slice(0, 8)}
    </Typography>
  );
}

export function FinancePage() {
  const [searchParams, setSearchParams] = useSearchParams();

  const rawTab = searchParams.get("tab") ?? "sales";
  const tab: FinanceTab = TABS.some((t) => t.key === rawTab) ? (rawTab as FinanceTab) : "sales";
  const urlBusinessId = searchParams.get("businessId") ?? "";
  const fromDate = searchParams.get("fromDate") ?? "";
  const toDate = searchParams.get("toDate") ?? "";
  const statusParam = searchParams.get("status") ?? "";
  const urlEmployeeId = searchParams.get("employeeId") ?? "";
  const page = Math.max(1, Number(searchParams.get("page")) || 1);
  const pageSize = Math.min(200, Math.max(25, Number(searchParams.get("pageSize")) || 50));

  const [businessInput, setBusinessInput] = useState(urlBusinessId);
  const [businessError, setBusinessError] = useState<string | null>(null);
  const [employeeInput, setEmployeeInput] = useState(urlEmployeeId);
  const [employeeError, setEmployeeError] = useState<string | null>(null);
  const [snack, setSnack] = useState<{ severity: "success" | "error" | "warning"; message: string } | null>(
    null,
  );

  /** URL query guncellemesi; filtre degisiklikleri sayfayi 1'e cevirir. */
  const updateParams = (patch: Record<string, string | null>, resetPage = true) => {
    const next = new URLSearchParams(searchParams);
    for (const [key, value] of Object.entries(patch)) {
      if (value === null || value === "") next.delete(key);
      else next.set(key, value);
    }
    if (resetPage) next.delete("page");
    setSearchParams(next);
  };

  /** GUID filtre uygulamasi: gecersiz giriste filtre uygulanmaz. */
  const applyGuidFilter = (key: "businessId" | "employeeId", label: string) => {
    const raw = key === "businessId" ? businessInput : employeeInput;
    const value = raw.trim();
    const clearError = key === "businessId" ? setBusinessError : setEmployeeError;
    if (value === "") {
      clearError(null);
      if (searchParams.get(key)) updateParams({ [key]: null });
      return;
    }
    if (!isGuid(value)) {
      clearError(`Gecerli bir ${label} kimligi (GUID) girin.`);
      setSnack({ severity: "warning", message: `Gecersiz ${label} kimligi; filtre uygulanmadi.` });
      return;
    }
    clearError(null);
    updateParams({ [key]: value });
  };

  // === Sorgular (anahtarlar modul onekiyle: ["finance", ...]) ===
  const dateFiltersActive = DATE_FILTER_TABS.includes(tab);
  const isRefundTab = tab === "refunds";
  const commonFilters = {
    businessId: urlBusinessId || undefined,
    fromDate: dateFiltersActive ? fromDate || undefined : undefined,
    toDate: dateFiltersActive ? toDate || undefined : undefined,
  };

  // Satis ucu liste + toplam tek nesne doner; getList degil getItem kullanilir.
  const salesQuery = useQuery({
    queryKey: ["finance", "sales", { ...commonFilters, page, pageSize }],
    queryFn: ({ signal }) =>
      getItem<AdminSalesListDto>(`/finance/sales${toQueryString({ ...commonFilters, page, pageSize })}`, signal),
    enabled: tab === "sales",
  });

  const paymentsQuery = useQuery({
    queryKey: ["finance", "payments", { ...commonFilters, isRefund: isRefundTab, page, pageSize }],
    queryFn: ({ signal }) =>
      getList<PaymentRow>(
        `/finance/payments${toQueryString({ ...commonFilters, isRefund: isRefundTab, page, pageSize })}`,
        signal,
      ),
    enabled: tab === "payments" || tab === "refunds",
  });

  const cashStatus = statusParam === "0" || statusParam === "1" ? Number(statusParam) : undefined;
  const cashQuery = useQuery({
    queryKey: ["finance", "cash-sessions", { businessId: urlBusinessId || undefined, status: cashStatus, page, pageSize }],
    queryFn: ({ signal }) =>
      getList<CashSessionRow>(
        `/finance/cash-sessions${toQueryString({ businessId: urlBusinessId || undefined, status: cashStatus, page, pageSize })}`,
        signal,
      ),
    enabled: tab === "cash",
  });

  const expensesQuery = useQuery({
    queryKey: ["finance", "expenses", { ...commonFilters, page, pageSize }],
    queryFn: ({ signal }) =>
      getList<ExpenseRow>(`/finance/expenses${toQueryString({ ...commonFilters, page, pageSize })}`, signal),
    enabled: tab === "expenses",
  });

  const commissionStatus = ["0", "1", "2"].includes(statusParam) ? Number(statusParam) : undefined;
  const appliedEmployeeId = isGuid(urlEmployeeId) ? urlEmployeeId : undefined;
  const commissionsQuery = useQuery({
    queryKey: [
      "finance",
      "commissions",
      { businessId: urlBusinessId || undefined, employeeId: appliedEmployeeId, status: commissionStatus, page, pageSize },
    ],
    queryFn: ({ signal }) =>
      getList<CommissionRow>(
        `/finance/commissions${toQueryString({
          businessId: urlBusinessId || undefined,
          employeeId: appliedEmployeeId,
          status: commissionStatus,
          page,
          pageSize,
        })}`,
        signal,
      ),
    enabled: tab === "commissions",
  });

  // Aktif sekmenin hatasi Snackbar ile duyurulur; tablo icinde ErrorState
  // ayrica gosterilir. Bildirim render'dan turetilir (effect'siz): ayni hata
  // mesaji kapatildiginda kayit altina alinir, yeni hata tekrar acar.
  const activeQueryError =
    tab === "sales"
      ? salesQuery.error
      : tab === "payments" || tab === "refunds"
        ? paymentsQuery.error
        : tab === "cash"
          ? cashQuery.error
          : tab === "expenses"
            ? expensesQuery.error
            : commissionsQuery.error;

  const [dismissedQueryError, setDismissedQueryError] = useState<string | null>(null);
  const activeQueryErrorMessage =
    activeQueryError === null || activeQueryError === undefined
      ? null
      : activeQueryError instanceof ApiError
        ? activeQueryError.userMessage
        : "Veri alinamadi. Lutfen tekrar deneyin.";
  const errorSnack =
    activeQueryErrorMessage !== null && dismissedQueryError !== activeQueryErrorMessage
      ? { severity: "error" as const, message: activeQueryErrorMessage }
      : null;
  const activeSnack = snack ?? errorSnack;

  const closeSnack = () => {
    if (snack !== null) setSnack(null);
    if (errorSnack !== null) setDismissedQueryError(errorSnack.message);
  };

  const onTabChange = (_: SyntheticEvent, value: string) => {
    updateParams({ tab: value === "sales" ? null : value });
  };

  const onPageChange = (nextPage: number) => updateParams({ page: String(nextPage) }, false);
  const onPageSizeChange = (nextPageSize: number) => updateParams({ pageSize: String(nextPageSize) });

  // === Sutun tanimlari ===
  const salesColumns: DataTableColumn<SaleRow>[] = [
    { key: "soldAt", header: "Tarih", render: (row) => formatDateTime(row.soldAt) },
    { key: "businessId", header: "Isletme", render: (row) => <GuidText value={row.businessId} /> },
    { key: "subtotal", header: "Ara Toplam", render: (row) => formatMoney(row.subtotal, row.currency) },
    { key: "discountTotal", header: "Indirim", render: (row) => formatMoney(row.discountTotal, row.currency) },
    { key: "taxTotal", header: "Vergi", render: (row) => formatMoney(row.taxTotal, row.currency) },
    {
      key: "grandTotal",
      header: "Genel Toplam",
      render: (row) => <strong>{formatMoney(row.grandTotal, row.currency)}</strong>,
    },
    {
      key: "status",
      header: "Durum",
      render: (row) => <StatusBadge status={labelFor(row.status, SALE_STATUS_LABELS)} />,
    },
  ];

  const paymentColumns: DataTableColumn<PaymentRow>[] = [
    { key: "paidAt", header: "Tarih", render: (row) => formatDateTime(row.paidAt) },
    { key: "businessId", header: "Isletme", render: (row) => <GuidText value={row.businessId} /> },
    {
      key: "paymentMethod",
      header: "Odeme Yontemi",
      render: (row) => labelFor(row.paymentMethod, PAYMENT_METHOD_LABELS),
    },
    { key: "amount", header: "Tutar", render: (row) => formatMoney(row.amount, row.currency) },
    ...(isRefundTab
      ? [
          {
            key: "refersToPaymentId",
            header: "Iade Edilen Odeme",
            render: (row: PaymentRow) =>
              row.refersToPaymentId ? <GuidText value={row.refersToPaymentId} /> : "-",
          },
        ]
      : []),
    {
      key: "status",
      header: "Durum",
      render: (row) => <StatusBadge status={labelFor(row.status, SALE_STATUS_LABELS)} />,
    },
  ];

  const cashColumns: DataTableColumn<CashSessionRow>[] = [
    { key: "openedAt", header: "Acilis", render: (row) => formatDateTime(row.openedAt) },
    { key: "closedAt", header: "Kapanis", render: (row) => (row.closedAt ? formatDateTime(row.closedAt) : "-") },
    {
      key: "openingCash",
      header: "Acilis Nakit",
      render: (row) => formatMoney(row.openingCash),
    },
    {
      key: "expectedCash",
      header: "Beklenen",
      render: (row) => (row.expectedCash === null || row.expectedCash === undefined ? "-" : formatMoney(row.expectedCash)),
    },
    {
      key: "countedCash",
      header: "Sayilan",
      render: (row) => (row.countedCash === null || row.countedCash === undefined ? "-" : formatMoney(row.countedCash)),
    },
    {
      key: "difference",
      header: "Fark",
      render: (row) => {
        if (row.difference === null || row.difference === undefined) {
          return <Typography color="text.secondary">Kapanmadi</Typography>;
        }
        // Fark isaretle birlikte verilir; pozitif yesil, negatif kirmizi.
        const color =
          row.difference > 0 ? "success.main" : row.difference < 0 ? "error.main" : "text.primary";
        return (
          <Typography component="span" color={color} sx={{ fontWeight: 600 }}>
            {formatSignedMoney(row.difference)}
          </Typography>
        );
      },
    },
    {
      key: "status",
      header: "Durum",
      render: (row) => <StatusBadge status={labelFor(row.status, CASH_SESSION_STATUS_LABELS)} />,
    },
  ];

  const expenseColumns: DataTableColumn<ExpenseRow>[] = [
    { key: "expenseDate", header: "Tarih", render: (row) => formatDateTime(row.expenseDate) },
    { key: "businessId", header: "Isletme", render: (row) => <GuidText value={row.businessId} /> },
    {
      key: "description",
      header: "Aciklama",
      render: (row) => row.description ?? "-",
    },
    {
      key: "paymentMethod",
      header: "Odeme Yontemi",
      render: (row) => labelFor(row.paymentMethod, PAYMENT_METHOD_LABELS),
    },
    { key: "amount", header: "Tutar", render: (row) => formatMoney(row.amount, row.currency) },
    {
      key: "status",
      header: "Durum",
      render: (row) => <StatusBadge status={labelFor(row.status, SALE_STATUS_LABELS)} />,
    },
  ];

  const commissionColumns: DataTableColumn<CommissionRow>[] = [
    { key: "calculatedAt", header: "Tarih", render: (row) => formatDateTime(row.calculatedAt) },
    { key: "businessId", header: "Isletme", render: (row) => <GuidText value={row.businessId} /> },
    { key: "employeeId", header: "Calisan", render: (row) => <GuidText value={row.employeeId} /> },
    { key: "baseAmount", header: "Taban Tutar", render: (row) => formatMoney(row.baseAmount) },
    {
      key: "commissionAmount",
      header: "Prim Tutari",
      render: (row) => <strong>{formatMoney(row.commissionAmount)}</strong>,
    },
    {
      key: "status",
      header: "Durum",
      render: (row) => <StatusBadge status={labelFor(row.status, COMMISSION_STATUS_LABELS)} />,
    },
  ];

  // === Aktif sekmenin tablosu ===
  let tableContent: ReactElement;
  if (tab === "sales") {
    const sales = salesQuery.data;
    tableContent = (
      <DataTable<SaleRow>
        ariaLabel="satis listesi"
        columns={salesColumns}
        rows={sales?.items}
        loading={salesQuery.isPending}
        error={salesQuery.error}
        page={page}
        pageSize={pageSize}
        totalCount={sales?.totalCount ?? 0}
        onPageChange={onPageChange}
        onPageSizeChange={onPageSizeChange}
        emptyLabel="Bu filtrelerle satis kaydi bulunamadi."
        onRefresh={() => salesQuery.refetch()}
      />
    );
  } else if (tab === "payments" || tab === "refunds") {
    const payments = paymentsQuery.data;
    tableContent = (
      <DataTable<PaymentRow>
        ariaLabel={isRefundTab ? "iade listesi" : "tahsilat listesi"}
        columns={paymentColumns}
        rows={payments?.items}
        loading={paymentsQuery.isPending}
        error={paymentsQuery.error}
        page={page}
        pageSize={pageSize}
        totalCount={payments?.pagination.totalCount ?? 0}
        onPageChange={onPageChange}
        onPageSizeChange={onPageSizeChange}
        emptyLabel={isRefundTab ? "Bu filtrelerle iade kaydi bulunamadi." : "Bu filtrelerle tahsilat kaydi bulunamadi."}
        onRefresh={() => paymentsQuery.refetch()}
      />
    );
  } else if (tab === "cash") {
    const sessions = cashQuery.data;
    tableContent = (
      <DataTable<CashSessionRow>
        ariaLabel="kasa oturum listesi"
        columns={cashColumns}
        rows={sessions?.items}
        loading={cashQuery.isPending}
        error={cashQuery.error}
        page={page}
        pageSize={pageSize}
        totalCount={sessions?.pagination.totalCount ?? 0}
        onPageChange={onPageChange}
        onPageSizeChange={onPageSizeChange}
        emptyLabel="Bu filtrelerle kasa oturumu bulunamadi."
        onRefresh={() => cashQuery.refetch()}
      />
    );
  } else if (tab === "expenses") {
    const expenses = expensesQuery.data;
    tableContent = (
      <DataTable<ExpenseRow>
        ariaLabel="gider listesi"
        columns={expenseColumns}
        rows={expenses?.items}
        loading={expensesQuery.isPending}
        error={expensesQuery.error}
        page={page}
        pageSize={pageSize}
        totalCount={expenses?.pagination.totalCount ?? 0}
        onPageChange={onPageChange}
        onPageSizeChange={onPageSizeChange}
        emptyLabel="Bu filtrelerle gider kaydi bulunamadi."
        onRefresh={() => expensesQuery.refetch()}
      />
    );
  } else {
    const commissions = commissionsQuery.data;
    tableContent = (
      <DataTable<CommissionRow>
        ariaLabel="komisyon listesi"
        columns={commissionColumns}
        rows={commissions?.items}
        loading={commissionsQuery.isPending}
        error={commissionsQuery.error}
        page={page}
        pageSize={pageSize}
        totalCount={commissions?.pagination.totalCount ?? 0}
        onPageChange={onPageChange}
        onPageSizeChange={onPageSizeChange}
        emptyLabel="Bu filtrelerle prim tahakkuku bulunamadi."
        onRefresh={() => commissionsQuery.refetch()}
      />
    );
  }

  return (
    <Box>
      <PageHeader title="Finans" />
      <Tabs
        value={tab}
        onChange={onTabChange}
        aria-label="finans sekmeleri"
        variant="scrollable"
        allowScrollButtonsMobile
      >
        {TABS.map((item) => (
          <Tab key={item.key} value={item.key} label={item.label} />
        ))}
      </Tabs>
      <Divider />

      {/* Ortak filtre cubugu: isletme GUID + sekmeye ozel filtreler */}
      <Stack
        direction={{ xs: "column", md: "row" }}
        spacing={2}
        sx={{ alignItems: { xs: "stretch", md: "flex-start" }, my: 2 }}
      >
        <TextField
          label="Isletme Kimligi (GUID)"
          size="small"
          sx={{ minWidth: { md: 300 } }}
          value={businessInput}
          onChange={(e) => setBusinessInput(e.target.value)}
          onBlur={() => applyGuidFilter("businessId", "isletme")}
          error={businessError !== null}
          helperText={businessError ?? "Bos birakilirsa tum isletmeler."}
        />
        {dateFiltersActive ? (
          <>
            <TextField
              label="Baslangic Tarihi"
              type="date"
              size="small"
              sx={{ minWidth: { md: 180 } }}
              value={fromDate}
              onChange={(e) => updateParams({ fromDate: e.target.value })}
              slotProps={{ inputLabel: { shrink: true } }}
            />
            <TextField
              label="Bitis Tarihi"
              type="date"
              size="small"
              sx={{ minWidth: { md: 180 } }}
              value={toDate}
              onChange={(e) => updateParams({ toDate: e.target.value })}
              slotProps={{ inputLabel: { shrink: true } }}
            />
          </>
        ) : null}
        {tab === "cash" ? (
          <TextField
            select
            label="Kasa Durumu"
            size="small"
            sx={{ minWidth: { md: 160 } }}
            value={statusParam}
            onChange={(e) => updateParams({ status: e.target.value })}
          >
            <MenuItem value="">Tumu</MenuItem>
            {CASH_STATUS_OPTIONS.map((option) => (
              <MenuItem key={option.value} value={option.value}>
                {option.label}
              </MenuItem>
            ))}
          </TextField>
        ) : null}
        {tab === "commissions" ? (
          <>
            <TextField
              select
              label="Prim Durumu"
              size="small"
              sx={{ minWidth: { md: 180 } }}
              value={statusParam}
              onChange={(e) => updateParams({ status: e.target.value })}
            >
              <MenuItem value="">Tumu</MenuItem>
              {COMMISSION_STATUS_OPTIONS.map((option) => (
                <MenuItem key={option.value} value={option.value}>
                  {option.label}
                </MenuItem>
              ))}
            </TextField>
            <TextField
              label="Calisan Kimligi (GUID)"
              size="small"
              sx={{ minWidth: { md: 300 } }}
              value={employeeInput}
              onChange={(e) => setEmployeeInput(e.target.value)}
              onBlur={() => applyGuidFilter("employeeId", "calisan")}
              error={employeeError !== null}
              helperText={employeeError ?? "Bos birakilirsa tum calisanlar."}
            />
          </>
        ) : null}
      </Stack>

      {/* Satis sekmesinde filtre kapsaminin toplam cirosu yanitta tek nesne olarak gelir */}
      {tab === "sales" && salesQuery.data ? (
        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
          Filtre kapsamindaki toplam ciro:{" "}
          <strong>{formatMoney(salesQuery.data.totalGrandTotal ?? 0)}</strong> ({salesQuery.data.totalCount} kayit)
        </Typography>
      ) : null}

      {tableContent}

      <Snackbar
        open={activeSnack !== null}
        autoHideDuration={4000}
        onClose={closeSnack}
        anchorOrigin={{ vertical: "bottom", horizontal: "center" }}
      >
        <Alert severity={activeSnack?.severity} variant="filled" onClose={closeSnack}>
          {activeSnack?.message}
        </Alert>
      </Snackbar>
    </Box>
  );
}
