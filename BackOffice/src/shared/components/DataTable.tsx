import { Box, Button, Table, TableBody, TableCell, TableContainer, TableHead, TablePagination, TableRow } from "@mui/material";
import type { ReactNode } from "react";
import { EmptyState, ErrorState, LoadingState } from "./states";

/**
 * Ortak veri tablosu (doküman 22): server-side sayfalama, loading/empty/error
 * durumları ve URL senkronu. Büyük kümeler tamına indirilmez — sayfalama
 * sunucudan gelir (pagination.totalCount).
 */
export interface DataTableColumn<T> {
  key: string;
  header: string;
  render: (row: T) => ReactNode;
  width?: string;
}

export function DataTable<T extends { id: string }>({
  columns,
  rows,
  loading,
  error,
  page,
  pageSize,
  totalCount,
  onPageChange,
  onPageSizeChange,
  emptyLabel,
  rowActions,
  ariaLabel,
  onRefresh,
}: {
  columns: DataTableColumn<T>[];
  rows: T[] | undefined;
  loading: boolean;
  error: unknown;
  page: number;
  pageSize: number;
  totalCount: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
  emptyLabel?: string;
  rowActions?: (row: T) => ReactNode;
  ariaLabel: string;
  onRefresh?: () => void;
}) {
  if (loading) return <LoadingState />;
  if (error) return <ErrorState error={error} />;

  const rowsSafe = rows ?? [];
  const actionColumn = rowActions ? 1 : 0;

  return (
    <Box>
      <TableContainer sx={{ maxHeight: 640 }}>
        <Table stickyHeader aria-label={ariaLabel} size="small">
          <TableHead>
            <TableRow>
              {columns.map((column) => (
                <TableCell key={column.key} style={{ width: column.width, fontWeight: 600 }}>
                  {column.header}
                </TableCell>
              ))}
              {actionColumn ? <TableCell align="right">İşlemler</TableCell> : null}
            </TableRow>
          </TableHead>
          <TableBody>
            {rowsSafe.length === 0 ? (
              <TableRow>
                <TableCell colSpan={columns.length + actionColumn}>
                  <EmptyState label={emptyLabel ?? "Kayıt bulunamadı."} />
                </TableCell>
              </TableRow>
            ) : (
              rowsSafe.map((row) => (
                <TableRow key={row.id} hover>
                  {columns.map((column) => (
                    <TableCell key={column.key}>{column.render(row)}</TableCell>
                  ))}
                  {actionColumn ? <TableCell align="right">{rowActions?.(row)}</TableCell> : null}
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </TableContainer>
      <Box
        sx={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          gap: 1,
          flexWrap: "wrap",
        }}
      >
        <Button size="small" onClick={() => onRefresh?.()}>
          Yenile
        </Button>
        <TablePagination
          component="div"
          count={totalCount}
          page={page - 1}
          onPageChange={(_, newPage) => onPageChange(newPage + 1)}
          rowsPerPage={pageSize}
          onRowsPerPageChange={(e) => onPageSizeChange(Number(e.target.value) || 50)}
          rowsPerPageOptions={[25, 50, 100, 200]}
          labelDisplayedRows={({ from, to, count }) => `${from}-${to} / ${count}`}
          // Mobilde "sayfa başına satır" etiketi ve select gizlenir; dar
          // ekranda sayfalama altligi tek satıra sigar.
          sx={{
            "& .MuiTablePagination-selectLabel, & .MuiTablePagination-input": {
              display: { xs: "none", md: "revert" },
            },
          }}
        />
      </Box>
    </Box>
  );
}
