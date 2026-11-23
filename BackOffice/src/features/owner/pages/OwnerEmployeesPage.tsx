import { useState } from "react";
import type { SyntheticEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Alert,
  Box,
  Button,
  MenuItem,
  Snackbar,
  Stack,
  Tab,
  Tabs,
  TextField,
  Typography,
} from "@mui/material";
import { ApiError } from "../../../shared/api/client";
import { toQueryString } from "../../../shared/api/types";
import { DataTable } from "../../../shared/components/DataTable";
import type { DataTableColumn } from "../../../shared/components/DataTable";
import { ReasonConfirmDialog } from "../../../shared/components/ReasonConfirmDialog";
import { StatusBadge } from "../../../shared/components/StatusBadge";
import { EmptyState, ErrorState, LoadingState, PageHeader } from "../../../shared/components/states";
import { useAuth } from "../../../app/providers/AuthContext";
import { OWNER_PERMISSIONS } from "../../../shared/constants/permissions";
import { ownerGetList, ownerPostItem, useOwnedBusinesses } from "../api/ownerClient";
import type { OwnerEmployeeListItem, OwnerTimeOffListItem } from "../types";
import {
  TIME_OFF_STATUS_OPTIONS,
  activeText,
  formatDateTime,
  shortId,
  timeOffStatusLabel,
} from "../types";

/**
 * Owner çalışanları ve izin istekleri. İzin onayı/reddi gerekçe diyaloguyla
 * yapılır (reason formda doldurulur); işlem kuralları SUNUCUDA uygulanır.
 */

type EmployeesTab = "employees" | "timeoffs";
type PendingDecision = { id: string; action: "approve" | "reject" } | null;
type Feedback = { severity: "success" | "error"; message: string } | null;

const APPROVE_IMPACT =
  "İzin isteğini onaylamak çalışanın ilgili günlerde yeni randevu almasını engeller. Gerekçeniz kayda geçer.";
const REJECT_IMPACT =
  "İzin isteğini reddetmek isteği kapatır; çalışan ilgili günlerde randevu almaya devam eder. Gerekçeniz kayda geçer.";

function toErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof ApiError) return error.userMessage;
  if (error instanceof Error) return error.message;
  return fallback;
}

function GuidText({ value }: { value: string }) {
  return (
    <Typography variant="body2" component="span" title={value}>
      {shortId(value)}
    </Typography>
  );
}

export function OwnerEmployeesPage() {
  const queryClient = useQueryClient();
  const { has_permission } = useAuth();
  const canDecide = has_permission(OWNER_PERMISSIONS.employees);

  const businessesQuery = useOwnedBusinesses();
  const businesses = businessesQuery.data?.items ?? [];
  const [selectedId, setSelectedId] = useState("");
  const businessId = selectedId || businesses[0]?.id || "";

  const [tab, setTab] = useState<EmployeesTab>("employees");
  const [statusParam, setStatusParam] = useState("");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(50);
  const [pending, setPending] = useState<PendingDecision>(null);
  const [feedback, setFeedback] = useState<Feedback>(null);

  const statusFilter = statusParam === "" ? undefined : Number(statusParam);

  const employeesQuery = useQuery({
    queryKey: ["owner", "employees", businessId],
    queryFn: ({ signal }) =>
      ownerGetList<OwnerEmployeeListItem>(`/businesses/${businessId}/employees`, signal),
    enabled: businessId !== "" && tab === "employees",
  });

  // İzin geçmişi yalnızca ilgili sekme açıldığında çekilir.
  const timeOffsQuery = useQuery({
    queryKey: ["owner", "timeoffs", businessId, statusParam],
    queryFn: ({ signal }) =>
      ownerGetList<OwnerTimeOffListItem>(
        `/businesses/${businessId}/time-offs${toQueryString({ status: statusFilter })}`,
        signal,
      ),
    enabled: businessId !== "" && tab === "timeoffs",
  });

  const decideMutation = useMutation({
    mutationFn: ({ id, action, reason }: { id: string; action: "approve" | "reject"; reason: string }) =>
      ownerPostItem(`/businesses/${businessId}/time-offs/${id}/${action}`, { reason }),
    onSuccess: (_data, variables) => {
      setPending(null);
      setFeedback({
        severity: "success",
        message: variables.action === "approve" ? "İzin isteği onaylandı." : "İzin isteği reddedildi.",
      });
      void queryClient.invalidateQueries({ queryKey: ["owner", "timeoffs"] });
    },
    onError: (error: unknown) => {
      setFeedback({ severity: "error", message: toErrorMessage(error, "İşlem başarısız.") });
    },
  });

  const employeeColumns: DataTableColumn<OwnerEmployeeListItem>[] = [
    {
      key: "name",
      header: "Ad Soyad",
      render: (row) => `${row.firstName} ${row.lastName}`,
    },
    { key: "phoneNumber", header: "Telefon", render: (row) => row.phoneNumber },
    {
      key: "isActive",
      header: "Durum",
      render: (row) => <StatusBadge status={activeText(row.isActive)} />,
    },
  ];

  const timeOffColumns: DataTableColumn<OwnerTimeOffListItem>[] = [
    {
      key: "employee",
      header: "Çalışan",
      render: (row) => <GuidText value={row.employeeId} />,
    },
    { key: "startDate", header: "Başlangıç", render: (row) => formatDateTime(row.startDate) },
    { key: "endDate", header: "Bitiş", render: (row) => formatDateTime(row.endDate) },
    { key: "reason", header: "Gerekçe", render: (row) => row.reason ?? "-" },
    {
      key: "status",
      header: "Durum",
      render: (row) => <StatusBadge status={timeOffStatusLabel(row.status)} />,
    },
  ];

  const onTabChange = (_event: SyntheticEvent, value: string): void => {
    setTab(value as EmployeesTab);
    setPage(1);
  };

  if (businessesQuery.isLoading) return <LoadingState />;
  if (businessesQuery.isError) return <ErrorState error={businessesQuery.error} />;
  if (businesses.length === 0) {
    return (
      <Box>
        <PageHeader title="Çalışanlar" />
        <EmptyState label="Size ait işletme bulunamadı." />
      </Box>
    );
  }

  return (
    <Box>
      <PageHeader title="Çalışanlar" />
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
      </Stack>

      <Tabs value={tab} onChange={onTabChange} sx={{ borderBottom: 1, borderColor: "divider", mb: 2 }}>
        <Tab value="employees" label="Çalışanlar" />
        <Tab value="timeoffs" label="İzin Istekleri" />
      </Tabs>

      {tab === "employees" ? (
        <DataTable
          columns={employeeColumns}
          rows={employeesQuery.data?.items}
          loading={employeesQuery.isLoading}
          error={employeesQuery.error}
          page={page}
          pageSize={pageSize}
          totalCount={employeesQuery.data?.pagination.totalCount ?? 0}
          onPageChange={(nextPage) => setPage(nextPage)}
          onPageSizeChange={(nextSize) => {
            setPageSize(nextSize);
            setPage(1);
          }}
          emptyLabel="Çalışan bulunamadı."
          ariaLabel="işletme çalışanları tablosu"
          onRefresh={() => void employeesQuery.refetch()}
        />
      ) : (
        <>
          <Stack direction="row" spacing={2} sx={{ mb: 2, alignItems: "center", flexWrap: "wrap" }}>
            <TextField
              select
              size="small"
              label="Durum"
              value={statusParam}
              onChange={(e) => {
                setStatusParam(e.target.value);
                setPage(1);
              }}
              sx={{ minWidth: 160 }}
            >
              {TIME_OFF_STATUS_OPTIONS.map((option) => (
                <MenuItem key={option.value} value={option.value}>
                  {option.label}
                </MenuItem>
              ))}
            </TextField>
          </Stack>
          <DataTable
            columns={timeOffColumns}
            rows={timeOffsQuery.data?.items}
            loading={timeOffsQuery.isLoading}
            error={timeOffsQuery.error}
            page={page}
            pageSize={pageSize}
            totalCount={timeOffsQuery.data?.pagination.totalCount ?? 0}
            onPageChange={(nextPage) => setPage(nextPage)}
            onPageSizeChange={(nextSize) => {
              setPageSize(nextSize);
              setPage(1);
            }}
            emptyLabel="İzin isteği bulunamadı."
            ariaLabel="izin istekleri tablosu"
            onRefresh={() => void timeOffsQuery.refetch()}
            rowActions={
              canDecide
                ? (row) =>
                    row.status === 0 ? (
                      <Stack direction="row" spacing={1} sx={{ justifyContent: "flex-end" }}>
                        <Button
                          size="small"
                          color="success"
                          onClick={() => setPending({ id: row.id, action: "approve" })}
                        >
                          Onayla
                        </Button>
                        <Button
                          size="small"
                          color="error"
                          onClick={() => setPending({ id: row.id, action: "reject" })}
                        >
                          Red
                        </Button>
                      </Stack>
                    ) : null
                : undefined
            }
          />
        </>
      )}

      <ReasonConfirmDialog
        open={pending !== null}
        title={pending?.action === "approve" ? "İzin Istegini Onayla" : "İzin Istegini Reddet"}
        impact={pending?.action === "approve" ? APPROVE_IMPACT : REJECT_IMPACT}
        confirmLabel={pending?.action === "approve" ? "Onayla" : "Reddet"}
        busy={decideMutation.isPending}
        onCancel={() => setPending(null)}
        onConfirm={(reason) => {
          if (pending) decideMutation.mutate({ id: pending.id, action: pending.action, reason });
        }}
      />

      <Snackbar
        open={feedback !== null}
        autoHideDuration={5000}
        onClose={() => setFeedback(null)}
        anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
      >
        {feedback !== null ? (
          <Alert severity={feedback.severity} variant="filled" onClose={() => setFeedback(null)}>
            {feedback.message}
          </Alert>
        ) : undefined}
      </Snackbar>
    </Box>
  );
}
