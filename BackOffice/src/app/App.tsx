import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { RouterProvider } from "react-router-dom";
import { AuthProvider } from "./providers/AuthContext";
import { ThemeRoot } from "../theme";
import { router } from "./router/routes";

/**
 * Cache stratejisi (doküman 26.4): raporlar orta, operasyon
 * verileri kısa stale time ile tazeliğini korur.
 */
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 60_000,
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <ThemeRoot>
          <RouterProvider router={router} />
        </ThemeRoot>
      </AuthProvider>
    </QueryClientProvider>
  );
}
