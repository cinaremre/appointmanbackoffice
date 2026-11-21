import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogContent,
  DialogTitle,
  MenuItem,
  Snackbar,
  Stack,
  TextField,
  Typography,
  useMediaQuery,
  useTheme,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { useAuth } from "../../../app/providers/AuthContext";
import { ApiError, getItem, getList, postItem } from "../../../shared/api/client";
import { toQueryString } from "../../../shared/api/types";
import { DataTable } from "../../../shared/components/DataTable";
import type { DataTableColumn } from "../../../shared/components/DataTable";
import { ReasonConfirmDialog } from "../../../shared/components/ReasonConfirmDialog";
import { StatusBadge } from "../../../shared/components/StatusBadge";
import { ErrorState, LoadingState, PageHeader } from "../../../shared/components/states";
import { PERMISSIONS } from "../../../shared/constants/permissions";
import type { UserDetail, UserListItem, UserManageAction } from "../types";

/**
 * Kullanici listesi (dokuman 12): server-side sayfalama + filtreler,
 * detay diyalogu (profil + uyelikler) ve gerekceli yonetim aksiyonlari.
 * Yonetim butonlari admin.user.manage izni olmadan RENDER EDILMEZ;
 * nihai yetki kontrolu yine sunucudadir. Sifre/hash alani API'de
 * bulunmadigi icin UI'da hicbir yerde gosterilmez.
 */

type PendingManage = { userId: string; action: UserManageAction; userLabel: string };

const ACTION_ENDPOINTS: Record<UserManageAction, string> = {
  lock: "lock",
  unlock: "unlock",
  "revoke-sessions": "revoke-sessions",
};

const ACTION_TITLES: Record<UserManageAction, string> = {
  lock: "Kullanicinin kilitlenmesi",
  unlock: "Kullanici kilidinin acilmasi",
  "revoke-sessions": "Oturumlarin sonlandirilmasi",
};

const IMPACT_TEXTS: Record<UserManageAction, string> = {
  lock: "Kullanicinin hesabi kilitlenir ve sisteme girisi engellenir. Nihai kontrol sunucudadir; gerekceniz denetim kaydina yazilir.",
  unlock: "Kullanicinin hesap kilidi acilir ve girisi yeniden mumkun olur. Nihai kontrol sunucudadir; gerekceniz denetim kaydina yazilir.",
  "revoke-sessions": "Kullanicinin tum aktif oturumlari aninda sonlandirilir; acik sekmelerde tekrar giris istenecek. Gerekceniz denetim kaydina yazilir.",
};

function formatDate(value?: string | null): string {
  if (!value) return "-";
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "-" : date.toLocaleString("tr-TR");
}

/** ApiError'dan kullaniciya uygun mesaj + traceId uretir. */
function errorText(error: unknown): string {
  if (error instanceof ApiError) {
    return error.traceId ? `${error.userMessage} (traceId: ${error.traceId})` : error.userMessage;
  }
  return "Islem basarisiz. Lutfen tekrar deneyin.";
}

/** Detay diyalogu: GET /users/{id} — profil, uyelikler ve pozisyonlar. */
function UserDetailDialog({ userId, onClose }: { userId: string | null; onClose: () => void }) {
  const theme = useTheme();
  const is_mobile = useMediaQuery(theme.breakpoints.down("sm"));
  const detailQuery = useQuery({
    queryKey: ["users", "detail", userId],
    queryFn: (ctx) => getItem<UserDetail>(`/users/${userId!}`, ctx.signal),
    enabled: userId !== null,
  });

  const detail = detailQuery.data;

  return (
    <Dialog open={userId !== null} onClose={onClose} maxWidth="sm" fullWidth fullScreen={is_mobile}>
      <DialogTitle>Kullanici Detayi</DialogTitle>
      <DialogContent>
        {detailQuery.isLoading ? (
          <LoadingState />
        ) : detailQuery.isError ? (
          <ErrorState error={detailQuery.error} />
        ) : detail ? (
          <Stack spacing={2} sx={{ mt: 1 }}>
            <Stack spacing={0.5}>
              <Typography variant="subtitle2">Profil</Typography>
              <Typography>
                Ad Soyad: {detail.firstName} {detail.lastName}
              </Typography>
              <Typography>E-posta: {detail.email}</Typography>
              <Typography>Telefon: {detail.phoneNumber ?? "-"}</Typography>
              <Typography>Rol: {detail.role}</Typography>
              <Typography>
                Durum: {detail.isLockedOut ? <StatusBadge status="Locked" /> : <StatusBadge status="Active" />}
              </Typography>
              <Typography>Son Giris: {formatDate(detail.lastLoginDate)}</Typography>
              <Typography>Kayit Tarihi: {formatDate(detail.createdAt)}</Typography>
            </Stack>
            <Box>
              <Typography variant="subtitle2" gutterBottom>
                Sahip Olunan Isletmeler
              </Typography>
              {detail.ownedBusinesses.length === 0 ? (
                <Typography color="text.secondary">Kayit yok.</Typography>
              ) : (
                detail.ownedBusinesses.map((business) => (
                  <Typography key={business.id}>
                    {business.name} ({business.id})
                  </Typography>
                ))
              )}
            </Box>
            <Box>
              <Typography variant="subtitle2" gutterBottom>
                Calisan Pozisyonlari
              </Typography>
              {detail.employeePositions.length === 0 ? (
                <Typography color="text.secondary">Kayit yok.</Typography>
              ) : (
                detail.employeePositions.map((position) => (
                  <Typography key={`${position.employeeId}-${position.businessId}`}>
                    Calisan {position.employeeId} - Isletme {position.businessId}
                  </Typography>
                ))
              )}
            </Box>
          </Stack>
        ) : null}
      </DialogContent>
    </Dialog>
  );
}

export function UserListPage() {
  const { has_permission } = useAuth();
  const queryClient = useQueryClient();
  const canManage = has_permission(PERMISSIONS.userManage);

  // Filtre girisleri ayri tutulur; "Filtrele" ile uygulanir.
  const [searchInput, setSearchInput] = useState("");
  const [emailInput, setEmailInput] = useState("");
  const [isActiveInput, setIsActiveInput] = useState("");
  const [applied, setApplied] = useState({ search: "", email: "", isActive: "" });
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(50);

  const [detailId, setDetailId] = useState<string | null>(null);
  const [pending, setPending] = useState<PendingManage | null>(null);
  const [snackbar, setSnackbar] = useState<{ severity: "success" | "error"; message: string } | null>(null);

  const listQuery = useQuery({
    queryKey: ["users", "list", applied, page, pageSize],
    queryFn: (ctx) =>
      getList<UserListItem>(
        `/users${toQueryString({
          search: applied.search || undefined,
          email: applied.email || undefined,
          isActive: applied.isActive === "" ? undefined : applied.isActive === "true",
          page,
          pageSize,
        })}`,
        ctx.signal,
      ),
  });

  // Tum yonetim aksiyonlari body {reason} ile gider; gerekce zorunludur.
  const manageMutation = useMutation({
    mutationFn: ({ userId, action, reason }: PendingManage & { reason: string }) =>
      postItem(`/users/${userId}/${ACTION_ENDPOINTS[action]}`, { reason }),
    onSuccess: () => {
      setPending(null);
      setSnackbar({ severity: "success", message: "Islem uygulandi ve denetim kaydina yazildi." });
      void queryClient.invalidateQueries({ queryKey: ["users"] });
    },
    onError: (error: unknown) => {
      setSnackbar({ severity: "error", message: errorText(error) });
    },
  });

  const columns: DataTableColumn<UserListItem>[] = [
    {
      key: "name",
      header: "Ad Soyad",
      render: (row) => `${row.firstName} ${row.lastName}`,
    },
    { key: "email", header: "E-posta", render: (row) => row.email },
    { key: "role", header: "Rol", render: (row) => row.role },
    {
      key: "status",
      header: "Durum",
      render: (row) => (row.isLockedOut ? <StatusBadge status="Locked" /> : <StatusBadge status="Active" />),
    },
    { key: "lastLogin", header: "Son Giris", render: (row) => formatDate(row.lastLoginDate) },
  ];

  const applyFilters = () => {
    setApplied({ search: searchInput.trim(), email: emailInput.trim(), isActive: isActiveInput });
    setPage(1);
  };

  return (
    <Box>
      <PageHeader title="Kullanicilar" />

      <Stack
        direction={{ xs: "column", md: "row" }}
        spacing={2}
        sx={{ mb: 2, alignItems: { xs: "stretch", md: "center" } }}
      >
        <TextField
          size="small"
          label="Arama (ad)"
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
          sx={{ minWidth: { xs: 0, sm: 220 } }}
        />
        <TextField
          size="small"
          label="E-posta filtresi"
          value={emailInput}
          onChange={(e) => setEmailInput(e.target.value)}
          sx={{ minWidth: { xs: 0, sm: 220 } }}
        />
        <TextField
          select
          size="small"
          label="Hesap Durumu"
          value={isActiveInput}
          onChange={(e) => setIsActiveInput(e.target.value)}
          sx={{ minWidth: { xs: 0, sm: 160 } }}
        >
          <MenuItem value="">Tumu</MenuItem>
          <MenuItem value="true">Aktif</MenuItem>
          <MenuItem value="false">Aktif Olmayan</MenuItem>
        </TextField>
        <Button variant="contained" onClick={applyFilters}>
          Filtrele
        </Button>
      </Stack>

      <DataTable
        columns={columns}
        rows={listQuery.data?.items}
        loading={listQuery.isLoading}
        error={listQuery.error}
        page={page}
        pageSize={pageSize}
        totalCount={listQuery.data?.pagination.totalCount ?? 0}
        onPageChange={(nextPage) => setPage(nextPage)}
        onPageSizeChange={(nextSize) => {
          setPageSize(nextSize);
          setPage(1);
        }}
        emptyLabel="Kayitli kullanici bulunamadi."
        ariaLabel="kullanicilar tablosu"
        onRefresh={() => void listQuery.refetch()}
        rowActions={(row) => {
          const userLabel = `${row.firstName} ${row.lastName}`;
          return (
            <Stack direction="row" spacing={1} sx={{ justifyContent: "flex-end" }}>
              <Button size="small" onClick={() => setDetailId(row.id)}>
                Detay
              </Button>
              {canManage ? (
                <>
                  {row.isLockedOut ? (
                    <Button
                      size="small"
                      color="primary"
                      onClick={() => setPending({ userId: row.id, action: "unlock", userLabel })}
                    >
                      Ac
                    </Button>
                  ) : (
                    <Button
                      size="small"
                      color="warning"
                      onClick={() => setPending({ userId: row.id, action: "lock", userLabel })}
                    >
                      Kilitle
                    </Button>
                  )}
                  <Button
                    size="small"
                    color="error"
                    onClick={() => setPending({ userId: row.id, action: "revoke-sessions", userLabel })}
                  >
                    Oturumlari Sonlandir
                  </Button>
                </>
              ) : null}
            </Stack>
          );
        }}
      />

      <UserDetailDialog userId={detailId} onClose={() => setDetailId(null)} />

      <ReasonConfirmDialog
        open={pending !== null}
        title={pending ? `${ACTION_TITLES[pending.action]}: ${pending.userLabel}` : ""}
        impact={pending ? IMPACT_TEXTS[pending.action] : ""}
        busy={manageMutation.isPending}
        onCancel={() => setPending(null)}
        onConfirm={(reason) => {
          if (pending) manageMutation.mutate({ ...pending, reason });
        }}
      />

      {snackbar ? (
        <Snackbar open autoHideDuration={6000} onClose={() => setSnackbar(null)}>
          <Alert severity={snackbar.severity} onClose={() => setSnackbar(null)}>
            {snackbar.message}
          </Alert>
        </Snackbar>
      ) : null}
    </Box>
  );
}
