import { useState } from "react";
import type { SyntheticEvent } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  Box,
  Button,
  Divider,
  MenuItem,
  Stack,
  Tab,
  Tabs,
  TextField,
  Typography,
} from "@mui/material";
import { toQueryString } from "../../../shared/api/types";
import { DataTable } from "../../../shared/components/DataTable";
import type { DataTableColumn } from "../../../shared/components/DataTable";
import { EmptyState, ErrorState, LoadingState, PageHeader } from "../../../shared/components/states";
import { StatusBadge } from "../../../shared/components/StatusBadge";
import { ownerGetList, useOwnedBusinesses } from "../api/ownerClient";
import type {
  OwnerCustomerPackageListItem,
  OwnerProductListItem,
  OwnerStockMovementListItem,
} from "../types";
import {
  activeText,
  formatDateOnly,
  formatDateTime,
  formatMoney,
  movementTypeLabel,
  packageStatusLabel,
  shortId,
} from "../types";

/**
 * Owner stok sayfasi. Sekmeler: Urunler / Hareketler / Paketler.
 * Tamami salt okunur; stok giris/cikis ve satis islemleri kasa akisindan
 * domain komutlariyla yurutulur. Urun bakiyesi minimum altina dustugunde
 * metin rengiyle vurgulanir (renk tek basina gosterge degildir).
 */

const STOCK_TABS = [
  { key: "products", label: "Urunler" },
  { key: "movements", label: "Hareketler" },
  { key: "packages", label: "Paketler" },
] as const;

type StockTab = (typeof STOCK_TABS)[number]["key"];

function GuidText({ value }: { value: string }) {
  return (
    <Typography variant="body2" component="span" title={value}>
      {shortId(value)}
    </Typography>
  );
}

export function OwnerStockPage() {
  const businessesQuery = useOwnedBusinesses();
  const businesses = businessesQuery.data?.items ?? [];
  const [selectedId, setSelectedId] = useState("");
  const businessId = selectedId || businesses[0]?.id || "";

  const [tab, setTab] = useState<StockTab>("products");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(50);

  // Hareket filtresi: GUID opsiyoneldir; bos birakilinca tum hareketler.
  const [movementProductInput, setMovementProductInput] = useState("");
  const [movementProductFilter, setMovementProductFilter] = useState("");

  const productsQuery = useQuery({
    queryKey: ["owner", "stock", "products", businessId, { page, pageSize }],
    queryFn: ({ signal }) =>
      ownerGetList<OwnerProductListItem>(
        `/businesses/${businessId}/stock/products${toQueryString({ page, pageSize })}`,
        signal,
      ),
    enabled: businessId !== "" && tab === "products",
  });

  const movementsQuery = useQuery({
    queryKey: ["owner", "stock", "movements", businessId, { movementProductFilter, page, pageSize }],
    queryFn: ({ signal }) =>
      ownerGetList<OwnerStockMovementListItem>(
        `/businesses/${businessId}/stock/movements${toQueryString({
          productId: movementProductFilter || undefined,
          page,
          pageSize,
        })}`,
        signal,
      ),
    enabled: businessId !== "" && tab === "movements",
  });

  const packagesQuery = useQuery({
    queryKey: ["owner", "packages", businessId, { page, pageSize }],
    queryFn: ({ signal }) =>
      ownerGetList<OwnerCustomerPackageListItem>(
        `/businesses/${businessId}/packages${toQueryString({ page, pageSize })}`,
        signal,
      ),
    enabled: businessId !== "" && tab === "packages",
  });

  const onPageChange = (nextPage: number) => setPage(nextPage);
  const onPageSizeChange = (nextPageSize: number) => {
    setPageSize(nextPageSize);
    setPage(1);
  };

  const onTabChange = (_event: SyntheticEvent, value: string): void => {
    setTab(value as StockTab);
    setPage(1);
  };

  // === Sutun tanimlari ===
  const productColumns: DataTableColumn<OwnerProductListItem>[] = [
    { key: "sku", header: "SKU", render: (row) => row.sku },
    { key: "name", header: "Urun", render: (row) => row.name },
    { key: "salePrice", header: "Satis Fiyati", render: (row) => formatMoney(row.salePrice) },
    { key: "minimumStock", header: "Min Stok", render: (row) => String(row.minimumStock) },
    {
      key: "balance",
      header: "Bakiye",
      render: (row) => (
        <Typography
          component="span"
          sx={{ fontWeight: 600, color: row.balance <= row.minimumStock ? "warning.dark" : undefined }}
        >
          {row.balance}
        </Typography>
      ),
    },
    {
      key: "isActive",
      header: "Durum",
      render: (row) => <StatusBadge status={activeText(row.isActive)} />,
    },
  ];

  const movementColumns: DataTableColumn<OwnerStockMovementListItem>[] = [
    { key: "productId", header: "Urun", render: (row) => <GuidText value={row.productId} /> },
    {
      key: "movementType",
      header: "Tur",
      render: (row) => movementTypeLabel(row.movementType),
    },
    { key: "quantity", header: "Miktar", render: (row) => String(row.quantity) },
    { key: "unitCost", header: "Birim Maliyet", render: (row) => formatMoney(row.unitCost) },
    { key: "referenceType", header: "Referans", render: (row) => row.referenceType ?? "-" },
    { key: "occurredAt", header: "Tarih", render: (row) => formatDateTime(row.occurredAt) },
  ];

  const packageColumns: DataTableColumn<OwnerCustomerPackageListItem>[] = [
    { key: "customerId", header: "Musteri", render: (row) => <GuidText value={row.customerId} /> },
    { key: "packageId", header: "Paket", render: (row) => <GuidText value={row.packageId} /> },
    { key: "purchasedAt", header: "Alim Tarihi", render: (row) => formatDateOnly(row.purchasedAt) },
    { key: "expiresAt", header: "Bitis Tarihi", render: (row) => formatDateOnly(row.expiresAt) },
    {
      key: "status",
      header: "Durum",
      render: (row) => <StatusBadge status={packageStatusLabel(row.status)} />,
    },
    { key: "purchased", header: "Alinan", render: (row) => String(row.purchased) },
    { key: "consumed", header: "Tuketilen", render: (row) => String(row.consumed) },
    { key: "reserved", header: "Rezerve", render: (row) => String(row.reserved) },
    { key: "remaining", header: "Kalan Hak", render: (row) => <strong>{row.remaining}</strong> },
  ];

  if (businessesQuery.isLoading) return <LoadingState />;
  if (businessesQuery.isError) return <ErrorState error={businessesQuery.error} />;
  if (businesses.length === 0) {
    return (
      <Box>
        <PageHeader title="Stok ve Paketler" />
        <EmptyState label="Size ait isletme bulunamadi." />
      </Box>
    );
  }

  return (
    <Box>
      <PageHeader title="Stok ve Paketler" />
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

      <Tabs value={tab} onChange={onTabChange} aria-label="stok sekmeleri">
        {STOCK_TABS.map((item) => (
          <Tab key={item.key} value={item.key} label={item.label} />
        ))}
      </Tabs>
      <Divider />

      {tab === "products" ? (
        <Box sx={{ mt: 2 }}>
          <DataTable
            columns={productColumns}
            rows={productsQuery.data?.items}
            loading={productsQuery.isPending}
            error={productsQuery.error}
            page={page}
            pageSize={pageSize}
            totalCount={productsQuery.data?.pagination.totalCount ?? 0}
            onPageChange={onPageChange}
            onPageSizeChange={onPageSizeChange}
            emptyLabel="Urun bulunamadi."
            ariaLabel="urun listesi"
            onRefresh={() => void productsQuery.refetch()}
          />
        </Box>
      ) : null}

      {tab === "movements" ? (
        <Box sx={{ mt: 2 }}>
          <Stack direction="row" spacing={2} sx={{ mb: 2, alignItems: "center", flexWrap: "wrap" }}>
            <TextField
              size="small"
              label="Urun Kimligi (GUID, opsiyonel)"
              value={movementProductInput}
              onChange={(e) => setMovementProductInput(e.target.value)}
              sx={{ minWidth: 300 }}
            />
            <Button
              variant="outlined"
              onClick={() => {
                setMovementProductFilter(movementProductInput.trim());
                setPage(1);
              }}
            >
              Filtrele
            </Button>
            <Button
              color="inherit"
              onClick={() => {
                setMovementProductInput("");
                setMovementProductFilter("");
                setPage(1);
              }}
            >
              Temizle
            </Button>
          </Stack>
          <DataTable
            columns={movementColumns}
            rows={movementsQuery.data?.items}
            loading={movementsQuery.isPending}
            error={movementsQuery.error}
            page={page}
            pageSize={pageSize}
            totalCount={movementsQuery.data?.pagination.totalCount ?? 0}
            onPageChange={onPageChange}
            onPageSizeChange={onPageSizeChange}
            emptyLabel="Stok hareketi bulunamadi."
            ariaLabel="stok hareketleri tablosu"
            onRefresh={() => void movementsQuery.refetch()}
          />
        </Box>
      ) : null}

      {tab === "packages" ? (
        <Box sx={{ mt: 2 }}>
          <DataTable
            columns={packageColumns}
            rows={packagesQuery.data?.items}
            loading={packagesQuery.isPending}
            error={packagesQuery.error}
            page={page}
            pageSize={pageSize}
            totalCount={packagesQuery.data?.pagination.totalCount ?? 0}
            onPageChange={onPageChange}
            onPageSizeChange={onPageSizeChange}
            emptyLabel="Musteri paketi bulunamadi."
            ariaLabel="musteri paketleri tablosu"
            onRefresh={() => void packagesQuery.refetch()}
          />
        </Box>
      ) : null}
    </Box>
  );
}
