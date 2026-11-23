import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { format } from "date-fns";
import { Link as RouterLink, useNavigate, useSearchParams } from "react-router-dom";
import {
  Box,
  Button,
  FormControl,
  Grid,
  InputLabel,
  MenuItem,
  Select,
  TextField,
  Typography,
} from "@mui/material";
import type { SelectChangeEvent } from "@mui/material/Select";
import { getList } from "../../../shared/api/client";
import { toQueryString } from "../../../shared/api/types";
import { DataTable } from "../../../shared/components/DataTable";
import type { DataTableColumn } from "../../../shared/components/DataTable";
import { PageHeader } from "../../../shared/components/states";
import { StatusBadge } from "../../../shared/components/StatusBadge";
import type { BusinessListFilters, BusinessListItem } from "../types";

/**
 * İŞLETME listesi (doküman 9 / doküman 22).
 * Sayfalama sunucudadır; status/search filtreleri URL query'de korunur,
 * böylece geri/ileri gezinme ve link paylaşımı filtreleri korur.
 */

const STATUS_OPTIONS: Array<{ value: string; label: string }> = [
  { value: "", label: "Tümü" },
  { value: "Active", label: "Aktif" },
  { value: "Suspended", label: "Askıda" },
];

function formatDate(value: string): string {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : format(date, "dd.MM.yyyy");
}

const COLUMNS: Array<DataTableColumn<BusinessListItem>> = [
  {
    key: "name",
    header: "İşletme Adı",
    render: (row) => (
      <RouterLink to={`/businesses/${row.id}`} style={{ color: "inherit", fontWeight: 600 }}>
        {row.name}
      </RouterLink>
    ),
  },
  { key: "status", header: "Durum", render: (row) => <StatusBadge status={row.status} /> },
  { key: "cityId", header: "Şehir", render: (row) => row.cityId },
  {
    key: "rating",
    header: "Puan",
    render: (row) =>
      Number.isFinite(row.rating)
        ? `${row.rating.toFixed(1)} (${row.totalReviews} değerlendirme)`
        : "Puanlanmadı",
  },
  { key: "createdAt", header: "Kayıt Tarihi", render: (row) => formatDate(row.createdAt) },
];

export function BusinessListPage() {
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();

  // Doküman 22: filtreler URL'de yaşamalı; geçersiz/yok değerler "" olarak normalize edilir.
  const filters = useMemo<BusinessListFilters>(
    () => ({
      status: searchParams.get("status") ?? "",
      search: searchParams.get("search") ?? "",
    }),
    [searchParams],
  );

  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(50);
  const [searchInput, setSearchInput] = useState(filters.search);

  // Filtre degistiginde sayfa numarasi bastan başlar.
  const prevFiltersRef = useRef(filters);
  useEffect(() => {
    if (prevFiltersRef.current !== filters) {
      prevFiltersRef.current = filters;
      setPage(1);
    }
  }, [filters]);

  // URL query'ye filtre yazar; boş değer parametreyi tamamen kaldırır.
  const patchParams = useCallback(
    (patch: Record<string, string>) => {
      const next = new URLSearchParams(searchParams);
      for (const [key, value] of Object.entries(patch)) {
        if (value === "") next.delete(key);
        else next.set(key, value);
      }
      setSearchParams(next, { replace: true });
    },
    [searchParams, setSearchParams],
  );

  // Arama kutusu: yazma durdugunda URL query'ye yazılır (debounce).
  useEffect(() => {
    if (searchInput === filters.search) return;
    const timer = window.setTimeout(() => {
      patchParams({ search: searchInput.trim() });
    }, 400);
    return () => window.clearTimeout(timer);
  }, [searchInput, filters.search, patchParams]);

  // URL başka bir yoldan değişirse (geri/ileri gezinme) kutu senkron kalır.
  useEffect(() => {
    setSearchInput(filters.search);
  }, [filters.search]);

  const query = useQuery({
    queryKey: ["businesses", filters, page, pageSize],
    queryFn: ({ signal }) =>
      getList<BusinessListItem>(
        `/businesses${toQueryString({
          status: filters.status,
          search: filters.search,
          page,
          pageSize,
        })}`,
        signal,
      ),
    placeholderData: keepPreviousData,
  });

  const onStatusChange = (event: SelectChangeEvent<string>) => {
    patchParams({ status: event.target.value });
  };

  const renderRowActions = (row: BusinessListItem) => (
    <Button size="small" variant="outlined" onClick={() => navigate(`/businesses/${row.id}`)}>
      Detay
    </Button>
  );

  return (
    <Box>
      <PageHeader
        title="İşletmeler"
        actions={
          query.data ? (
            <Typography variant="body2" color="text.secondary">
              Toplam: {query.data.pagination.totalCount}
            </Typography>
          ) : null
        }
      />
      <Grid container spacing={2} sx={{ mb: 2, alignItems: "flex-end" }}>
        <Grid size={{ xs: 12, sm: 6, md: 4 }}>
          <TextField
            size="small"
            fullWidth
            label="İşletme ara (ad)"
            value={searchInput}
            onChange={(event) => setSearchInput(event.target.value)}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <FormControl fullWidth size="small">
            <InputLabel id="business-status-filter-label">Durum</InputLabel>
            <Select
              labelId="business-status-filter-label"
              id="business-status-filter"
              value={filters.status}
              label="Durum"
              onChange={onStatusChange}
            >
              {STATUS_OPTIONS.map((option) => (
                <MenuItem key={option.value || "all"} value={option.value}>
                  {option.label}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        </Grid>
      </Grid>
      <DataTable<BusinessListItem>
        ariaLabel="işletme listesi"
        columns={COLUMNS}
        rows={query.data?.items}
        loading={query.isLoading}
        error={query.error}
        page={page}
        pageSize={pageSize}
        totalCount={query.data?.pagination.totalCount ?? 0}
        onPageChange={setPage}
        onPageSizeChange={setPageSize}
        onRefresh={() => void query.refetch()}
        emptyLabel="Filtreye uygun işletme bulunamadı."
        rowActions={renderRowActions}
      />
    </Box>
  );
}
