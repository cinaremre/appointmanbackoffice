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
 * Kritik islem onay diyalogu (dokuman 5.4/24): gerekce zorunludur,
 * etki metni acikca yazilir; onaysiz islem gonderilmez.
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
  /** Islemin etkisini anlatan, backend kurallariyla uyumlu metin. */
  impact: string;
  confirmLabel?: string;
  busy?: boolean;
  onCancel: () => void;
  onConfirm: (reason: string) => void;
}) {
  const [reason, setReason] = useState("");
  const [touched, setTouched] = useState(false);
  const invalid = touched && reason.trim().length < 3;

  // Mobilde diyalog tam ekran acilir; baslik yaninda kapatma ikonu bulunur.
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
          label="Gerekce (zorunlu)"
          fullWidth
          multiline
          minRows={2}
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          onBlur={() => setTouched(true)}
          error={invalid}
          helperText={invalid ? "En az 3 karakter girin." : "Bu islem denetim kaydina yazilir."}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={onCancel} disabled={busy}>
          Vazgec
        </Button>
        <Button
          onClick={() => onConfirm(reason.trim())}
          disabled={busy || reason.trim().length < 3}
          color="primary"
          variant="contained"
        >
          {busy ? "Isleniyor..." : confirmLabel}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
