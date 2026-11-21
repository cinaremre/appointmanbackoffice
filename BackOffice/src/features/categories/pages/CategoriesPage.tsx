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
 * Kategori yonetimi (dokuman 10). Backend duz liste doner; frontend
 * kucuk agaci kurar ve MUI List icinde girintiyle gosterir.
 */

type NotifySeverity = "success" | "warning" | "error";
type Notify = (message: string, severity?: NotifySeverity) => void;

function errorMessage(error: unknown): string {
  return error instanceof ApiError ? error.userMessage : "Beklenmedik bir hata olustu. Lutfen tekrar deneyin.";
}

/** Duz listeden agac kurar: root'lar once, cocuklar parent'in altinda sirali. */
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

  // Render icin duz sirayla (DFS) derinlik bilgisiyle dondur.
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

/** Olusturma ve duzenleme formlarinin ortak semasi (react-hook-form + zod). */
const categoryFormSchema = z.object({
  name: z
    .string()
    .trim()
    .min(2, "Kategori adi en az 2 karakter olmali.")
    .max(100, "Kategori adi en fazla 100 karakter olabilir."),
  displayOrder: z
    .string()
    .trim()
    .refine((value) => value === "" || /^[0-9]+$/.test(value), "Gosterim sirasi yalnizca sayi olmali."),
  isActive: z.boolean(),
});
type CategoryFormValues = z.infer<typeof categoryFormSchema>;

/** Yeni kategori / alt kategori formu. Mount aninda taze baslar. */
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
      notify("Kategori olusturuldu.");
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
            <TextField label="Ust Kategori" value={parent.name} fullWidth margin="dense" disabled />
          ) : null}
          <TextField
            label="Kategori Adi"
            fullWidth
            margin="dense"
            error={!!errors.name}
            helperText={errors.name?.message}
            {...register("name")}
          />
          <TextField
            label="Gosterim Sirasi (opsiyonel)"
            fullWidth
            margin="dense"
            error={!!errors.displayOrder}
            helperText={errors.displayOrder?.message ?? "Ayni seviyede kucuk deger ustte gorunur."}
            {...register("displayOrder")}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose} disabled={createMutation.isPending}>
            Vazgec
          </Button>
          <Button type="submit" variant="contained" disabled={createMutation.isPending}>
            {createMutation.isPending ? "Kaydediliyor..." : "Kaydet"}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
}

/** Kategori duzenleme formu (PATCH). Degerler dugumden ilk deger olarak gelir. */
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
      notify("Kategori guncellendi.");
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
      <DialogTitle>Kategoriyi Duzenle: {node.name}</DialogTitle>
      <form onSubmit={submit} noValidate>
        <DialogContent>
          <TextField
            label="Kategori Adi"
            fullWidth
            margin="dense"
            error={!!errors.name}
            helperText={errors.name?.message}
            {...register("name")}
          />
          <TextField
            label="Gosterim Sirasi"
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
            Vazgec
          </Button>
          <Button type="submit" variant="contained" disabled={updateMutation.isPending}>
            {updateMutation.isPending ? "Kaydediliyor..." : "Kaydet"}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
}

/** Tasima dialog'u: hedef parent secimi. Dongu icin alt agac secilemez. */
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

  // Tasinan dugum ve tum alt agaci hedef olarak secilemez (dongu korumasi).
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
      notify("Kategori tasinildi.");
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
      <DialogTitle>Tasi: {node.name}</DialogTitle>
      <DialogContent>
        <FormControl fullWidth margin="dense">
          <InputLabel id="move-target-label">Hedef Ust Kategori</InputLabel>
          <Select
            labelId="move-target-label"
            label="Hedef Ust Kategori"
            value={target}
            onChange={(event) => setTarget(event.target.value)}
          >
            <MenuItem value="">Kok seviye (ust kategori yok)</MenuItem>
            {options.map((item) => (
              <MenuItem key={item.id} value={item.id}>
                {`${"-".repeat(item.depth)} ${item.name}`}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        <Typography variant="caption" color="text.secondary" sx={{ display: "block", mt: 1 }}>
          Kategori, sectiginiz ust kategorinin altina tasinir. Kendi alt agaci hedef secilemez.
        </Typography>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={moveMutation.isPending}>
          Vazgec
        </Button>
        <Button
          variant="contained"
          disabled={moveMutation.isPending}
          onClick={() => moveMutation.mutate(target === "" ? null : target)}
        >
          {moveMutation.isPending ? "Tasiniyor..." : "Tasi"}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

/** Arsivleme onayi: gerekce zorunlu; etki metni acikca yazilir (dokuman 24). */
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
      notify("Kategori arsivlendi.");
      onClose();
    },
    onError: (error: unknown) => notify(errorMessage(error), "error"),
  });

  const hasActiveChildren = node.children.some((child) => child.isActive);
  const impact = hasActiveChildren
    ? "Arsivlenen kategori listelerden kaybolur ve yeni atamalarda secilemez. Dikkat: bu kategorinin aktif cocuk kategorileri var; sunucu bu islemi reddeder. Once cocuklari arsivleyin."
    : "Arsivlenen kategori listelerden kaybolur ve yeni atamalarda secilemez. Aktif cocuk kategorisi varsa sunucu islemi reddeder (409).";

  return (
    <ReasonConfirmDialog
      open
      title={`Arsivle: ${node.name}`}
      impact={impact}
      confirmLabel="Arsivle"
      busy={archiveMutation.isPending}
      onCancel={onClose}
      onConfirm={(reason) => archiveMutation.mutate(reason)}
    />
  );
}

/** Kullanim dialog'u: kategorideki isletme sayisi (GET /categories/{id}/usage). */
function UsageDialog({ categoryId, onClose }: { categoryId: string; onClose: () => void }) {
  const theme = useTheme();
  const is_mobile = useMediaQuery(theme.breakpoints.down("sm"));
  const usage = useQuery({
    queryKey: ["categories", "usage", categoryId],
    queryFn: (context) => getItem<CategoryUsageDto>(`/categories/${categoryId}/usage`, context.signal),
  });

  return (
    <Dialog open onClose={onClose} maxWidth="xs" fullWidth fullScreen={is_mobile}>
      <DialogTitle>Kullanim Bilgisi</DialogTitle>
      <DialogContent>
        {usage.isPending ? (
          <LoadingState label="Kullanim bilgisi yukleniyor..." />
        ) : usage.isError ? (
          <ErrorState error={usage.error} />
        ) : (
          <Stack spacing={1} sx={{ mt: 1 }}>
            <Typography>
              Kategori: <strong>{usage.data?.name}</strong>
            </Typography>
            <Typography>
              Bu kategoride <strong>{usage.data?.businessCount}</strong> isletme kayitli.
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

  // undefined = kapali; null = root create; CategoryDto = alt kategori create.
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
        <EmptyState label="Kategori bulunamadi." />
      ) : (
        <List aria-label="kategori agaci">
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
                      Duzenle
                    </Button>
                  ) : null}
                  {canUpdate ? (
                    <Button size="small" onClick={() => setMoveNode(node)}>
                      Tasi
                    </Button>
                  ) : null}
                  {canArchive ? (
                    <Button size="small" color="warning" onClick={() => setArchiveNode(node)}>
                      Arsivle
                    </Button>
                  ) : null}
                  <Button size="small" onClick={() => setUsageId(node.id)}>
                    Kullanim
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
                secondary={`Sira: ${node.displayOrder} - Cocuk kategori: ${node.childCount}`}
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
