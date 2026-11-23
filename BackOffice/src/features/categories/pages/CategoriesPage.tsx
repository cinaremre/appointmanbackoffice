import { useMemo, useState } from "react";
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  FormControlLabel,
  InputLabel,
  List,
  ListItem,
  ListItemText,
  MenuItem,
  Select,
  Snackbar,
  Stack,
  Switch,
  TextField,
  Typography,
  useMediaQuery,
  useTheme,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { zodResolver } from "@hookform/resolvers/zod";
import { Controller, useForm } from "react-hook-form";
import { z } from "zod";
import { ApiError, getItem, getList, patchItem, postItem } from "../../../shared/api/client";
import { EmptyState, ErrorState, LoadingState, PageHeader } from "../../../shared/components/states";
import { ReasonConfirmDialog } from "../../../shared/components/ReasonConfirmDialog";
import { StatusBadge } from "../../../shared/components/StatusBadge";
import { PERMISSIONS } from "../../../shared/constants/permissions";
import { useAuth } from "../../../app/providers/AuthContext";
import type { CategoryDto, CategoryNode, CategoryUsageDto } from "../types";

/**
 * Kategori yönetimi (doküman 10). Backend düz liste döner; frontend
 * küçük ağacı kurar ve MUI List içinde girintiyle gösterir.
 */

type NotifySeverity = "success" | "warning" | "error";
type Notify = (message: string, severity?: NotifySeverity) => void;

function errorMessage(error: unknown): string {
  return error instanceof ApiError ? error.userMessage : "Beklenmedik bir hata oluştu. Lütfen tekrar deneyin.";
}

/** Duz listeden ağaç kurar: root'lar önce, çocuklar parent'in altında sıralı. */
function buildCategoryTree(items: CategoryDto[]): CategoryNode[] {
  const nodes = new Map<string, CategoryNode>();
  for (const item of items) {
    nodes.set(item.id, { ...item, children: [], depth: 0 });
  }

  const roots: CategoryNode[] = [];
  for (const node of nodes.values()) {
    const parent = node.parentId ? nodes.get(node.parentId) : undefined;
    if (parent && parent.id !== node.id) parent.children.push(node);
    else roots.push(node);
  }

  const sortNodes = (list: CategoryNode[]) => {
    list.sort((a, b) => a.displayOrder - b.displayOrder || a.name.localeCompare(b.name, "tr"));
    for (const child of list) sortNodes(child.children);
  };
  sortNodes(roots);

  // Render için düz sırayla (DFS) derinlik bilgisiyle döndür.
  const flat: CategoryNode[] = [];
  const walk = (list: CategoryNode[], depth: number) => {
    for (const node of list) {
      node.depth = depth;
      flat.push(node);
      walk(node.children, depth + 1);
    }
  };
  walk(roots, 0);
  return flat;
}

/** Oluşturma ve düzenleme formlarinin ortak şeması (react-hook-form + zod). */
const categoryFormSchema = z.object({
  name: z
    .string()
    .trim()
    .min(2, "Kategori adı en az 2 karakter olmalı.")
    .max(100, "Kategori adı en fazla 100 karakter olabilir."),
  displayOrder: z
    .string()
    .trim()
    .refine((value) => value === "" || /^[0-9]+$/.test(value), "Gosterim sırası yalnızca sayı olmalı."),
  isActive: z.boolean(),
});
type CategoryFormValues = z.infer<typeof categoryFormSchema>;

/** Yeni kategori / alt kategori formu. Mount anında taze başlar. */
function CreateCategoryForm({
  parent,
  onClose,
  notify,
}: {
  parent: CategoryDto | null;
  onClose: () => void;
  notify: Notify;
}) {
  const queryClient = useQueryClient();
  const theme = useTheme();
  const is_mobile = useMediaQuery(theme.breakpoints.down("sm"));
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<CategoryFormValues>({
    resolver: zodResolver(categoryFormSchema),
    defaultValues: { name: "", displayOrder: "", isActive: true },
  });

  const createMutation = useMutation({
    mutationFn: (payload: { name: string; parentId?: string; displayOrder?: number }) =>
      postItem<CategoryDto>("/categories", payload),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["categories"] });
      notify("Kategori oluşturuldu.");
      onClose();
    },
    onError: (error: unknown) => notify(errorMessage(error), "error"),
  });

  const submit = handleSubmit((values) => {
    const payload: { name: string; parentId?: string; displayOrder?: number } = { name: values.name };
    if (parent) payload.parentId = parent.id;
    if (values.displayOrder !== "") payload.displayOrder = Number(values.displayOrder);
    createMutation.mutate(payload);
  });

  return (
    <Dialog
      open
      onClose={createMutation.isPending ? undefined : onClose}
      maxWidth="sm"
      fullWidth
      fullScreen={is_mobile}
    >
      <DialogTitle>{parent ? `Alt Kategori Ekle: ${parent.name}` : "Yeni Kategori"}</DialogTitle>
      <form onSubmit={submit} noValidate>
        <DialogContent>
          {parent ? (
            <TextField label="Üst Kategori" value={parent.name} fullWidth margin="dense" disabled />
          ) : null}
          <TextField
            label="Kategori Adı"
            fullWidth
            margin="dense"
            error={!!errors.name}
            helperText={errors.name?.message}
            {...register("name")}
          />
          <TextField
            label="Gosterim Sırası (opsiyonel)"
            fullWidth
            margin="dense"
            error={!!errors.displayOrder}
            helperText={errors.displayOrder?.message ?? "Ayni seviyede küçük değer üstte görünür."}
            {...register("displayOrder")}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose} disabled={createMutation.isPending}>
            Vazgeç
          </Button>
          <Button type="submit" variant="contained" disabled={createMutation.isPending}>
            {createMutation.isPending ? "Kaydediliyor..." : "Kaydet"}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
}

/** Kategori düzenleme formu (PATCH). Değerler dugumden ilk değer olarak gelir. */
function EditCategoryForm({
  node,
  onClose,
  notify,
}: {
  node: CategoryNode;
  onClose: () => void;
  notify: Notify;
}) {
  const queryClient = useQueryClient();
  const theme = useTheme();
  const is_mobile = useMediaQuery(theme.breakpoints.down("sm"));
  const {
    register,
    handleSubmit,
    control,
    formState: { errors },
  } = useForm<CategoryFormValues>({
    resolver: zodResolver(categoryFormSchema),
    defaultValues: {
      name: node.name,
      displayOrder: String(node.displayOrder),
      isActive: node.isActive,
    },
  });

  const updateMutation = useMutation({
    mutationFn: (payload: { name: string; displayOrder?: number; isActive: boolean }) =>
      patchItem<CategoryDto>(`/categories/${node.id}`, payload),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["categories"] });
      notify("Kategori güncellendi.");
      onClose();
    },
    onError: (error: unknown) => notify(errorMessage(error), "error"),
  });

  const submit = handleSubmit((values) => {
    const payload: { name: string; displayOrder?: number; isActive: boolean } = {
      name: values.name,
      isActive: values.isActive,
    };
    if (values.displayOrder !== "") payload.displayOrder = Number(values.displayOrder);
    updateMutation.mutate(payload);
  });

  return (
    <Dialog
      open
      onClose={updateMutation.isPending ? undefined : onClose}
      maxWidth="sm"
      fullWidth
      fullScreen={is_mobile}
    >
      <DialogTitle>Kategoriyi Düzenle: {node.name}</DialogTitle>
      <form onSubmit={submit} noValidate>
        <DialogContent>
          <TextField
            label="Kategori Adı"
            fullWidth
            margin="dense"
            error={!!errors.name}
            helperText={errors.name?.message}
            {...register("name")}
          />
          <TextField
            label="Gosterim Sırası"
            fullWidth
            margin="dense"
            error={!!errors.displayOrder}
            helperText={errors.displayOrder?.message}
            {...register("displayOrder")}
          />
          <Controller
            name="isActive"
            control={control}
            render={({ field }) => (
              <FormControlLabel
                control={<Switch checked={field.value} onChange={field.onChange} />}
                label="Aktif"
              />
            )}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose} disabled={updateMutation.isPending}>
            Vazgeç
          </Button>
          <Button type="submit" variant="contained" disabled={updateMutation.isPending}>
            {updateMutation.isPending ? "Kaydediliyor..." : "Kaydet"}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
}

/** Taşıma dialog'u: hedef parent seçimi. Dongu için alt ağaç seçilemez. */
function MoveCategoryForm({
  node,
  flat,
  onClose,
  notify,
}: {
  node: CategoryNode;
  flat: CategoryNode[];
  onClose: () => void;
  notify: Notify;
}) {
  const queryClient = useQueryClient();
  const [target, setTarget] = useState("");
  const theme = useTheme();
  const is_mobile = useMediaQuery(theme.breakpoints.down("sm"));

  // Tasinan düğüm ve tüm alt ağacı hedef olarak seçilemez (dongu korumasi).
  const excludedIds = useMemo(() => {
    const ids = new Set<string>();
    const collect = (current: CategoryNode) => {
      ids.add(current.id);
      current.children.forEach(collect);
    };
    collect(node);
    return ids;
  }, [node]);

  const options = flat.filter((item) => !excludedIds.has(item.id));

  const moveMutation = useMutation({
    mutationFn: (targetParentId: string | null) =>
      postItem(`/categories/${node.id}/move`, { targetParentId }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["categories"] });
      notify("Kategori taşınıldı.");
      onClose();
    },
    onError: (error: unknown) => notify(errorMessage(error), "error"),
  });

  return (
    <Dialog
      open
      onClose={moveMutation.isPending ? undefined : onClose}
      maxWidth="xs"
      fullWidth
      fullScreen={is_mobile}
    >
      <DialogTitle>Taşı: {node.name}</DialogTitle>
      <DialogContent>
        <FormControl fullWidth margin="dense">
          <InputLabel id="move-target-label">Hedef Üst Kategori</InputLabel>
          <Select
            labelId="move-target-label"
            label="Hedef Üst Kategori"
            value={target}
            onChange={(event) => setTarget(event.target.value)}
          >
            <MenuItem value="">Kök seviye (üst kategori yok)</MenuItem>
            {options.map((item) => (
              <MenuItem key={item.id} value={item.id}>
                {`${"-".repeat(item.depth)} ${item.name}`}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        <Typography variant="caption" color="text.secondary" sx={{ display: "block", mt: 1 }}>
          Kategori, seçtiğiniz üst kategorinin altına taşınır. Kendi alt ağacı hedef seçilemez.
        </Typography>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={moveMutation.isPending}>
          Vazgeç
        </Button>
        <Button
          variant="contained"
          disabled={moveMutation.isPending}
          onClick={() => moveMutation.mutate(target === "" ? null : target)}
        >
          {moveMutation.isPending ? "Taşınıyor..." : "Taşı"}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

/** Arşivleme onayı: gerekçe zorunlu; etki metni açıkça yazılır (doküman 24). */
function ArchiveCategoryDialog({
  node,
  onClose,
  notify,
}: {
  node: CategoryNode;
  onClose: () => void;
  notify: Notify;
}) {
  const queryClient = useQueryClient();

  const archiveMutation = useMutation({
    mutationFn: (reason: string) => postItem(`/categories/${node.id}/archive`, { reason }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["categories"] });
      notify("Kategori arşivlendi.");
      onClose();
    },
    onError: (error: unknown) => notify(errorMessage(error), "error"),
  });

  const hasActiveChildren = node.children.some((child) => child.isActive);
  const impact = hasActiveChildren
    ? "Arşivlenen kategori listelerden kaybolur ve yeni atamalarda seçilemez. Dikkat: bu kategorinin aktif çocuk kategorileri var; sunucu bu işlemi reddeder. Önce çocukları arşivleyin."
    : "Arşivlenen kategori listelerden kaybolur ve yeni atamalarda seçilemez. Aktif çocuk kategorisi varsa sunucu işlemi reddeder (409).";

  return (
    <ReasonConfirmDialog
      open
      title={`Arşivle: ${node.name}`}
      impact={impact}
      confirmLabel="Arşivle"
      busy={archiveMutation.isPending}
      onCancel={onClose}
      onConfirm={(reason) => archiveMutation.mutate(reason)}
    />
  );
}

/** Kullanım dialog'u: kategorideki işletme sayısı (GET /categories/{id}/usage). */
function UsageDialog({ categoryId, onClose }: { categoryId: string; onClose: () => void }) {
  const theme = useTheme();
  const is_mobile = useMediaQuery(theme.breakpoints.down("sm"));
  const usage = useQuery({
    queryKey: ["categories", "usage", categoryId],
    queryFn: (context) => getItem<CategoryUsageDto>(`/categories/${categoryId}/usage`, context.signal),
  });

  return (
    <Dialog open onClose={onClose} maxWidth="xs" fullWidth fullScreen={is_mobile}>
      <DialogTitle>Kullanım Bilgisi</DialogTitle>
      <DialogContent>
        {usage.isPending ? (
          <LoadingState label="Kullanım bilgisi yükleniyor..." />
        ) : usage.isError ? (
          <ErrorState error={usage.error} />
        ) : (
          <Stack spacing={1} sx={{ mt: 1 }}>
            <Typography>
              Kategori: <strong>{usage.data?.name}</strong>
            </Typography>
            <Typography>
              Bu kategoride <strong>{usage.data?.businessCount}</strong> işletme kayıtlı.
            </Typography>
          </Stack>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Kapat</Button>
      </DialogActions>
    </Dialog>
  );
}

export function CategoriesPage() {
  const { has_permission } = useAuth();
  const [snackbar, setSnackbar] = useState<{ open: boolean; message: string; severity: NotifySeverity }>({
    open: false,
    message: "",
    severity: "success",
  });
  const notify: Notify = (message, severity = "success") =>
    setSnackbar({ open: true, message, severity });

  // undefined = kapalı; null = root create; CategoryDto = alt kategori create.
  const [createParent, setCreateParent] = useState<CategoryDto | null | undefined>(undefined);
  const [editNode, setEditNode] = useState<CategoryNode | null>(null);
  const [moveNode, setMoveNode] = useState<CategoryNode | null>(null);
  const [archiveNode, setArchiveNode] = useState<CategoryNode | null>(null);
  const [usageId, setUsageId] = useState<string | null>(null);

  const categoriesQuery = useQuery({
    queryKey: ["categories"],
    queryFn: (context) => getList<CategoryDto>("/categories", context.signal),
  });

  const flat = useMemo(() => buildCategoryTree(categoriesQuery.data?.items ?? []), [categoriesQuery.data]);

  const canCreate = has_permission(PERMISSIONS.categoryCreate);
  const canUpdate = has_permission(PERMISSIONS.categoryUpdate);
  const canArchive = has_permission(PERMISSIONS.categoryArchive);

  const closeSnackbar = (_event?: unknown, reason?: string) => {
    if (reason === "clickaway") return;
    setSnackbar((prev) => ({ ...prev, open: false }));
  };

  return (
    <Box>
      <PageHeader
        title="Kategoriler"
        actions={
          canCreate ? (
            <Button variant="contained" onClick={() => setCreateParent(null)}>
              Yeni Kategori
            </Button>
          ) : null
        }
      />

      {categoriesQuery.isLoading ? (
        <LoadingState />
      ) : categoriesQuery.isError ? (
        <ErrorState error={categoriesQuery.error} />
      ) : flat.length === 0 ? (
        <EmptyState label="Kategori bulunamadı." />
      ) : (
        <List aria-label="kategori ağacı">
          {flat.map((node) => (
            <ListItem
              key={node.id}
              divider
              sx={{ pl: node.depth * 3 + 2 }}
              secondaryAction={
                <Stack
                  direction="row"
                  spacing={1}
                  useFlexGap
                  sx={{ flexWrap: "wrap", justifyContent: "flex-end", maxWidth: { xs: 200, sm: "100%" } }}
                >
                  {canCreate ? (
                    <Button size="small" onClick={() => setCreateParent(node)}>
                      Alt Kategori Ekle
                    </Button>
                  ) : null}
                  {canUpdate ? (
                    <Button size="small" onClick={() => setEditNode(node)}>
                      Düzenle
                    </Button>
                  ) : null}
                  {canUpdate ? (
                    <Button size="small" onClick={() => setMoveNode(node)}>
                      Taşı
                    </Button>
                  ) : null}
                  {canArchive ? (
                    <Button size="small" color="warning" onClick={() => setArchiveNode(node)}>
                      Arşivle
                    </Button>
                  ) : null}
                  <Button size="small" onClick={() => setUsageId(node.id)}>
                    Kullanım
                  </Button>
                </Stack>
              }
            >
              <ListItemText
                primary={
                  <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
                    <Typography component="span">{node.name}</Typography>
                    <StatusBadge status={node.isActive ? "Aktif" : "Pasif"} />
                  </Stack>
                }
                secondary={`Sıra: ${node.displayOrder} - Çocuk kategori: ${node.childCount}`}
              />
            </ListItem>
          ))}
        </List>
      )}

      {createParent !== undefined ? (
        <CreateCategoryForm
          key={createParent?.id ?? "root"}
          parent={createParent ?? null}
          onClose={() => setCreateParent(undefined)}
          notify={notify}
        />
      ) : null}
      {editNode ? <EditCategoryForm key={editNode.id} node={editNode} onClose={() => setEditNode(null)} notify={notify} /> : null}
      {moveNode ? (
        <MoveCategoryForm node={moveNode} flat={flat} onClose={() => setMoveNode(null)} notify={notify} />
      ) : null}
      {archiveNode ? (
        <ArchiveCategoryDialog node={archiveNode} onClose={() => setArchiveNode(null)} notify={notify} />
      ) : null}
      {usageId ? <UsageDialog categoryId={usageId} onClose={() => setUsageId(null)} /> : null}

      <Snackbar
        open={snackbar.open}
        autoHideDuration={4000}
        onClose={closeSnackbar}
        anchorOrigin={{ vertical: "bottom", horizontal: "center" }}
      >
        <Alert severity={snackbar.severity} variant="filled" onClose={() => closeSnackbar()}>
          {snackbar.message}
        </Alert>
      </Snackbar>
    </Box>
  );
}
