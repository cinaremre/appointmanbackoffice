import CloseIcon from "@mui/icons-material/Close";
import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  IconButton,
  TextField,
  useMediaQuery,
  useTheme,
} from "@mui/material";
import { useState } from "react";

/**
 * Kritik işlem onay diyalogu (doküman 5.4/24): gerekçe zorunludur,
 * etki metni açıkça yazılır; onaysız işlem gönderilmez.
 */
export function ReasonConfirmDialog({
  open,
  title,
  impact,
  confirmLabel = "Onayla",
  busy = false,
  onCancel,
  onConfirm,
}: {
  open: boolean;
  title: string;
  /** İşlemin etkisini anlatan, backend kurallarıyla uyumlu metin. */
  impact: string;
  confirmLabel?: string;
  busy?: boolean;
  onCancel: () => void;
  onConfirm: (reason: string) => void;
}) {
  const [reason, setReason] = useState("");
  const [touched, setTouched] = useState(false);
  const invalid = touched && reason.trim().length < 3;

  // Mobilde diyalog tam ekran açılır; başlık yanında kapatma ikonu bulunur.
  const theme = useTheme();
  const is_mobile = useMediaQuery(theme.breakpoints.down("sm"));

  return (
    <Dialog
      open={open}
      onClose={busy ? undefined : onCancel}
      maxWidth="sm"
      fullWidth
      fullScreen={is_mobile}
    >
      <DialogTitle sx={is_mobile ? { pr: 6 } : undefined}>
        {title}
        {is_mobile ? (
          <IconButton
            aria-label="kapat"
            onClick={onCancel}
            disabled={busy}
            sx={{ position: "absolute", right: 8, top: 8 }}
          >
            <CloseIcon />
          </IconButton>
        ) : null}
      </DialogTitle>
      <DialogContent>
        <DialogContentText style={{ marginTop: 8, marginBottom: 16 }}>{impact}</DialogContentText>
        <TextField
          autoFocus
          margin="dense"
          id="reason"
          label="Gerekçe (zorunlu)"
          fullWidth
          multiline
          minRows={2}
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          onBlur={() => setTouched(true)}
          error={invalid}
          helperText={invalid ? "En az 3 karakter girin." : "Bu işlem denetim kaydına yazılır."}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={onCancel} disabled={busy}>
          Vazgeç
        </Button>
        <Button
          onClick={() => onConfirm(reason.trim())}
          disabled={busy || reason.trim().length < 3}
          color="primary"
          variant="contained"
        >
          {busy ? "İşleniyor..." : confirmLabel}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
