import { CssBaseline } from "@mui/material";
import { ThemeProvider, createTheme } from "@mui/material/styles";
import type { ReactNode } from "react";

/**
 * Açık tema varsayılan (doküman 5.2). Semantik renkler MUI
 * palette'ina eşlenir; durum gösterimi yalnız renge dayali değil
 * (etiket metinleriyle birlikte kullanılır).
 */
const theme = createTheme({
  palette: {
    mode: "light",
    primary: { main: "#2d4b73" },
    secondary: { main: "#5f7a99" },
    success: { main: "#2e7d32" },
    warning: { main: "#ed6c02" },
    error: { main: "#c62828" },
    info: { main: "#0277bd" },
    background: { default: "#f4f6f8", paper: "#ffffff" },
  },
  typography: {
    fontFamily: [
      "Inter",
      "Segoe UI",
      "Roboto",
      "Arial",
      "sans-serif",
    ].join(","),
  },
  shape: { borderRadius: 8 },
  components: {
    MuiButton: { defaultProps: { disableElevation: true } },
    MuiTableHead: { styleOverrides: { root: { backgroundColor: "#eef1f5" } } },
  },
});

export function ThemeRoot({ children }: { children: ReactNode }) {
  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      {children}
    </ThemeProvider>
  );
}
