import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Box, Chip, MenuItem, Stack, TextField, Typography } from "@mui/material";
import { toQueryString } from "../../../shared/api/types";
import { DataTable } from "../../../shared/components/DataTable";
import type { DataTableColumn } from "../../../shared/components/DataTable";
import { EmptyState, ErrorState, LoadingState, PageHeader } from "../../../shared/components/states";
import { ownerGetList, useOwnedBusinesses } from "../api/ownerClient";
import type { OwnerReviewListItem } from "../types";
import { formatDateOnly, shortId } from "../types";

/**
 * Owner yorumlari: isletme kapsaminda musterilerin biriktirdigi yorumlar.
 * Puan yildiz metni olarak gosterilir ("4.5"); gizli yorumlar rozetle
 * isaretlenir. Yorum moderasyonu domain kurallariyla yapilir; bu sayfa
 * salt okunurdur.
 */

function GuidText({ value }: { value: string }) {
  return (
    <Typography variant="body2" component="span" title={value}>
      {shortId(value)}
    </Typography>
  );
}

export function OwnerReviewsPage() {
  const businessesQuery = useOwnedBusinesses();
  const businesses = businessesQuery.data?.items ?? [];
  const [selectedId, setSelectedId] = useState("");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(50);

  // Tek isletme otomatik secilir.
  const businessId = selectedId || businesses[0]?.id || "";

  const reviewsQuery = useQuery({
    queryKey: ["owner", "reviews", businessId, { page, pageSize }],
    queryFn: ({ signal }) =>
      ownerGetList<OwnerReviewListItem>(
        `/businesses/${businessId}/reviews${toQueryString({ page, pageSize })}`,
        signal,
      ),
    enabled: businessId !== "",
  });

  const columns: DataTableColumn<OwnerReviewListItem>[] = [
    { key: "customerId", header: "Musteri", render: (row) => <GuidText value={row.customerId} /> },
    { key: "rating", header: "Puan", render: (row) => row.rating.toFixed(1) },
    { key: "comment", header: "Yorum", render: (row) => row.comment ?? "-" },
    {
      key: "visibility",
      header: "Gorunurluk",
      render: (row) =>
        row.isHidden ? (
          <Chip size="small" label="Gizli" variant="outlined" />
        ) : (
          <Chip size="small" label="Yayinda" variant="outlined" color="success" />
        ),
    },
    { key: "createdAt", header: "Tarih", render: (row) => formatDateOnly(row.createdAt) },
  ];

  if (businessesQuery.isLoading) return <LoadingState />;
  if (businessesQuery.isError) return <ErrorState error={businessesQuery.error} />;
  if (businesses.length === 0) {
    return (
      <Box>
        <PageHeader title="Yorumlar" />
        <EmptyState label="Size ait isletme bulunamadi." />
      </Box>
    );
  }

  return (
    <Box>
      <PageHeader title="Yorumlar" />
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

      <DataTable
        columns={columns}
        rows={reviewsQuery.data?.items}
        loading={reviewsQuery.isLoading}
        error={reviewsQuery.error}
        page={page}
        pageSize={pageSize}
        totalCount={reviewsQuery.data?.pagination.totalCount ?? 0}
        onPageChange={(nextPage) => setPage(nextPage)}
        onPageSizeChange={(nextSize) => {
          setPageSize(nextSize);
          setPage(1);
        }}
        emptyLabel="Yorum bulunamadi."
        ariaLabel="isletme yorumlari tablosu"
        onRefresh={() => void reviewsQuery.refetch()}
      />
    </Box>
  );
}
