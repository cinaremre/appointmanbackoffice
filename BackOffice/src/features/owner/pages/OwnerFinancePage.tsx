import { useState } from "react";
import type { ReactElement, SyntheticEvent } from "react";
import { useQuery } from "@tanstack/react-query";
import { useSearchParams } from "react-router-dom";
import { Box, Divider, MenuItem, Stack, Tab, Tabs, TextField, Typography } from "@mui/material";
import { toQueryString } from "../../../shared/api/types";
import { DataTable } from "../../../shared/components/DataTable";
import type { DataTableColumn } from "../../../shared/components/DataTable";
import { EmptyState, ErrorState, LoadingState, PageHeader } from "../../../shared/components/states";
import { StatusBadge } from "../../../shared/components/StatusBadge";
import { ownerGetList, useOwnedBusinesses } from "../api/ownerClient";
import type { OwnerCashSessionListItem, OwnerPaymentListItem, OwnerSaleListItem } from "../types";
import {
  CASH_SESSION_STATUS_OPTIONS,
  cashSessionStatusLabel,
  formatDateTime,
  formatMoney,
  formatSignedMoney,
  paymentMethodLabel,
  saleStatusLabel,
  shortId,
} from "../types";

/**
 * Owner finans sayfasi. Tamami salt okunur: tutar duzeltmeleri domain
 * komutlariyla yapilir, arayuzden veri degistirilmez. Sekmeler:
 * Satislar / Tahsilatlar / Kasa. Filtreler (sekme, tarih, tur, durum)
 * URL query ile senkron; para bicimleri tr-TR.
 */

const FINANCE_TABS = [
  { key: "sales", label: "Satislar" },
  { key: "payments", label: "Tahsilatlar" },
  { key: "cash", label: "Kasa" },
] as const;

type FinanceTab = (typeof FINANCE_TABS)[number]["key"];

const REFUND_OPTIONS = [
  { value: "", label: "Tumu" },
  { value: "false", label: "Tahsilatlar" },
  { value: "true", label: "Iadeler" },
] as const;

function GuidText({ value }: { value: string }) {
  return (
    <Typography variant="body2" component="span" title={value}>
      {shortId(value)}
    </Typography>
  );
}

export function OwnerFinancePage() {
  const [searchParams, setSearchParams] = useSearchParams();

  const businessesQuery = useOwnedBusinesses();
  const businesses = businessesQuery.data?.items ?? [];
  const [selectedId, setSelectedId] = useState("");

  // Tek isletme otomatik secilir.
  const businessId = selectedId || businesses[0]?.id || "";

  const rawTab = searchParams.get("tab") ?? "sales";
  const tab: FinanceTab = FINANCE_TABS.some((t) => t.key === rawTab)
    ? (rawTab as FinanceTab)
    : "sales";
  const fromDate = searchParams.get("fromDate") ?? "";
  const toDate = searchParams.get("toDate") ?? "";
  const refundParam = searchParams.get("isRefund") ?? "";
  const cashStatusParam = searchParams.get("status") ?? "";
  const page = Math.max(1, Number(searchParams.get("page")) || 1);
  const pageSize = Math.min(200, Math.max(25, Number(searchParams.get("pageSize")) || 50));

  /** URL query guncellemesi; filtre degisiklikleri sayfayi 1'e cevirir. */
  const updateParams = (patch: Record<string, string | null>, resetPage = true) => {
    const next = new URLSearchParams(searchParams);
    for (const [key, value] of Object.entries(patch)) {
      if (value === null || value === "") next.delete(key);
      else next.set(key, value);
    }
    if (resetPage) next.delete("page");
    setSearchParams(next, { replace: true });
  };

  const isRefund = refundParam === "true" ? true : refundParam === "false" ? false : undefined;
  const cashStatus =
    cashStatusParam === "0" || cashStatusParam === "1" ? Number(cashStatusParam) : undefined;

  // === Sorgular: her sekme yalnizca acildiginda cekilir ===
  const salesQuery = useQuery({
    queryKey: ["owner", "finance", "sales", businessId, { fromDate, toDate, page, pageSize }],
    queryFn: ({ signal }) =>
      ownerGetList<OwnerSaleListItem>(
        `/businesses/${businessId}/finance/sales${toQueryString({
          fromDate: fromDate || undefined,
          toDate: toDate || undefined,
          page,
          pageSize,
        })}`,
        signal,
      ),
    enabled: businessId !== "" && tab === "sales",
  });

  const paymentsQuery = useQuery({
    queryKey: ["owner", "finance", "payments", businessId, { isRefund, page, pageSize }],
    queryFn: ({ signal }) =>
      ownerGetList<OwnerPaymentListItem>(
        `/businesses/${businessId}/finance/payments${toQueryString({ isRefund, page, pageSize })}`,
        signal,
      ),
    enabled: businessId !== "" && tab === "payments",
  });

  const cashQuery = useQuery({
    queryKey: ["owner", "finance", "cash", businessId, { cashStatus, page, pageSize }],
    queryFn: ({ signal }) =>
      ownerGetList<OwnerCashSessionListItem>(
        `/businesses/${businessId}/finance/cash-sessions${toQueryString({
          status: cashStatus,
          page,
          pageSize,
        })}`,
        signal,
      ),
    enabled: businessId !== "" && tab === "cash",
  });

  const onTabChange = (_: SyntheticEvent, value: string) => {
    updateParams({ tab: value === "sales" ? null : value });
  };

  const onPageChange = (nextPage: number) => updateParams({ page: String(nextPage) }, false);
  const onPageSizeChange = (nextPageSize: number) => updateParams({ pageSize: String(nextPageSize) });

  // === Sutun tanimlari ===
  const salesColumns: DataTableColumn<OwnerSaleListItem>[] = [
    { key: "soldAt", header: "Tarih", render: (row) => formatDateTime(row.soldAt) },
    {
      key: "grandTotal",
      header: "Genel Toplam",
      render: (row) => <strong>{formatMoney(row.grandTotal, row.currency)}</strong>,
    },
    {
      key: "status",
      header: "Durum",
      render: (row) => <StatusBadge status={saleStatusLabel(row.status)} />,
    },
  ];

  const paymentColumns: DataTableColumn<OwnerPaymentListItem>[] = [
    { key: "paidAt", header: "Tarih", render: (row) => formatDateTime(row.paidAt) },
    { key: "saleId", header: "Satis", render: (row) => <GuidText value={row.saleId} /> },
    {
      key: "paymentMethod",
      header: "Odeme Yontemi",
      render: (row) => paymentMethodLabel(row.paymentMethod),
    },
    { key: "amount", header: "Tutar", render: (row) => formatMoney(row.amount, row.currency) },
    {
      key: "kind",
      header: "Tur",
      render: (row) => <StatusBadge status={row.isRefund ? "Iade" : "Tahsilat"} />,
    },
  ];

  const cashColumns: DataTableColumn<OwnerCashSessionListItem>[] = [
    { key: "openedAt", header: "Acilis", render: (row) => formatDateTime(row.openedAt) },
    {
      key: "closedAt",
      header: "Kapanis",
      render: (row) => (row.closedAt ? formatDateTime(row.closedAt) : "-"),
    },
    { key: "openingCash", header: "Acilis Nakit", render: (row) => formatMoney(row.openingCash) },
    {
      key: "expectedCash",
      header: "Beklenen",
      render: (row) =>
        row.expectedCash === null || row.expectedCash === undefined
          ? "-"
          : formatMoney(row.expectedCash),
    },
    {
      key: "countedCash",
      header: "Sayilan",
      render: (row) =>
        row.countedCash === null || row.countedCash === undefined
          ? "-"
          : formatMoney(row.countedCash),
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
      render: (row) => <StatusBadge status={cashSessionStatusLabel(row.status)} />,
    },
  ];

  // === Aktif sekmenin tablosu ===
  let tableContent: ReactElement;
  if (tab === "sales") {
    const sales = salesQuery.data;
    tableContent = (
      <DataTable<OwnerSaleListItem>
        ariaLabel="satis listesi"
        columns={salesColumns}
        rows={sales?.items}
        loading={salesQuery.isPending}
        error={salesQuery.error}
        page={page}
        pageSize={pageSize}
        totalCount={sales?.pagination.totalCount ?? 0}
        onPageChange={onPageChange}
        onPageSizeChange={onPageSizeChange}
        emptyLabel="Bu filtrelerle satis kaydi bulunamadi."
        onRefresh={() => void salesQuery.refetch()}
      />
    );
  } else if (tab === "payments") {
    const payments = paymentsQuery.data;
    tableContent = (
      <DataTable<OwnerPaymentListItem>
        ariaLabel="tahsilat listesi"
        columns={paymentColumns}
        rows={payments?.items}
        loading={paymentsQuery.isPending}
        error={paymentsQuery.error}
        page={page}
        pageSize={pageSize}
        totalCount={payments?.pagination.totalCount ?? 0}
        onPageChange={onPageChange}
        onPageSizeChange={onPageSizeChange}
        emptyLabel="Bu filtrelerle tahsilat kaydi bulunamadi."
        onRefresh={() => void paymentsQuery.refetch()}
      />
    );
  } else {
    const sessions = cashQuery.data;
    tableContent = (
      <DataTable<OwnerCashSessionListItem>
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
        onRefresh={() => void cashQuery.refetch()}
      />
    );
  }

  if (businessesQuery.isLoading) return <LoadingState />;
  if (businessesQuery.isError) return <ErrorState error={businessesQuery.error} />;
  if (businesses.length === 0) {
    return (
      <Box>
        <PageHeader title="Finans" />
        <EmptyState label="Size ait isletme bulunamadi." />
      </Box>
    );
  }

  return (
    <Box>
      <PageHeader title="Finans" />
      <Tabs value={tab} onChange={onTabChange} aria-label="finans sekmeleri">
        {FINANCE_TABS.map((item) => (
          <Tab key={item.key} value={item.key} label={item.label} />
        ))}
      </Tabs>
      <Divider />

      {/* Ortak filtre cubugu: isletme secimi + sekmeye ozel filtreler */}
      <Stack
        direction={{ xs: "column", md: "row" }}
        spacing={2}
        sx={{ mb: 2, mt: 2, alignItems: { xs: "stretch", md: "center" }, flexWrap: "wrap" }}
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
        {tab === "sales" ? (
          <>
            <TextField
              label="Baslangic Tarihi"
              type="date"
              size="small"
              sx={{ minWidth: 180 }}
              value={fromDate}
              onChange={(e) => updateParams({ fromDate: e.target.value })}
              slotProps={{ inputLabel: { shrink: true } }}
            />
            <TextField
              label="Bitis Tarihi"
              type="date"
              size="small"
              sx={{ minWidth: 180 }}
              value={toDate}
              onChange={(e) => updateParams({ toDate: e.target.value })}
              slotProps={{ inputLabel: { shrink: true } }}
            />
          </>
        ) : null}
        {tab === "payments" ? (
          <TextField
            select
            label="Odeme Turu"
            size="small"
            sx={{ minWidth: 160 }}
            value={refundParam}
            onChange={(e) => updateParams({ isRefund: e.target.value })}
          >
            {REFUND_OPTIONS.map((option) => (
              <MenuItem key={option.value} value={option.value}>
                {option.label}
              </MenuItem>
            ))}
          </TextField>
        ) : null}
        {tab === "cash" ? (
          <TextField
            select
            label="Kasa Durumu"
            size="small"
            sx={{ minWidth: 160 }}
            value={cashStatusParam}
            onChange={(e) => updateParams({ status: e.target.value })}
          >
            <MenuItem value="">Tumu</MenuItem>
            {CASH_SESSION_STATUS_OPTIONS.map((option) => (
              <MenuItem key={option.value} value={option.value}>
                {option.label}
              </MenuItem>
            ))}
          </TextField>
        ) : null}
      </Stack>

      {tableContent}
    </Box>
  );
}
