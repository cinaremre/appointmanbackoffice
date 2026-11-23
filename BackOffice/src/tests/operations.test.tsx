import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { AuthProvider } from "../app/providers/AuthContext";
import { OperationsPage } from "../features/operations/pages/OperationsPage";

/**
 * Operasyon sayfası: sağlık kartları ve boş liste durumu. Sarmalayıcılar
 * gerçek uygulama provider'larıyla aynı sırada kurulur.
 */

function renderOperationsPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  return render(
    <AuthProvider>
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <OperationsPage />
        </MemoryRouter>
      </QueryClientProvider>
    </AuthProvider>,
  );
}

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), { status });
}

const emptyListEnvelope = {
  data: [],
  pagination: { page: 1, pageSize: 50, totalCount: 0, totalPages: 1, hasMore: false },
};

const healthyEnvelope = {
  data: { status: "Healthy", pendingJobs: 3, runningJobs: 1, failedJobs: 2, note: "Uygulama ayakta." },
};

function mockFetch(health: unknown, list: unknown) {
  globalThis.fetch = vi.fn(async (input: unknown) => {
    const url = String(input);
    if (url.includes("/operations/health")) {
      return jsonResponse(health);
    }
    return jsonResponse(list);
  }) as unknown as typeof fetch;
}

describe("OperationsPage", () => {
  const originalFetch = globalThis.fetch;

  beforeEach(() => {
    // Sahte oturum: AuthProvider sessionStorage'dan okur, istemci token ekler.
    sessionStorage.setItem(
      "backoffice.auth",
      JSON.stringify({
        token: "test-token",
        userId: "b18f4d2e-0000-0000-0000-000000000001",
        email: "admin@test.local",
        role: "PlatformAdmin",
      }),
    );
    sessionStorage.setItem("backoffice.token", "test-token");
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
    sessionStorage.clear();
    vi.restoreAllMocks();
  });

  it("sağlık kartlarında sistem durumu ve iş sayıları görünür", async () => {
    mockFetch(healthyEnvelope, emptyListEnvelope);

    renderOperationsPage();

    // Durum rozeti "Healthy" etiketini taşır (renk tek başına gösterge değil).
    await waitFor(() => {
      expect(screen.getByText("Healthy")).toBeInTheDocument();
    });
    expect(screen.getByText("Bekleyen İş")).toBeInTheDocument();
    expect(screen.getByText("İşlenen İş")).toBeInTheDocument();
    expect(screen.getByText("Başarısız İş")).toBeInTheDocument();
    expect(screen.getByText("3")).toBeInTheDocument();
  });

  it("iş listesi boşken EmptyState gösterilir", async () => {
    mockFetch(healthyEnvelope, emptyListEnvelope);

    renderOperationsPage();

    await waitFor(() => {
      expect(screen.getByText("Aktarım işi bulunamadı.")).toBeInTheDocument();
    });
  });
});
