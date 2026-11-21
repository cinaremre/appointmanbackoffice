import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { AuthProvider } from "../app/providers/AuthContext";
import { OperationsPage } from "../features/operations/pages/OperationsPage";

/**
 * Operasyon sayfasi: saglik kartlari ve bos liste durumu. Sarmalayicilar
 * gercek uygulama provider'lariyla ayni sirada kurulur.
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

  it("saglik kartlarinda sistem durumu ve is sayilari gorunur", async () => {
    mockFetch(healthyEnvelope, emptyListEnvelope);

    renderOperationsPage();

    // Durum rozeti "Healthy" etiketini tasir (renk tek basina gosterge degil).
    await waitFor(() => {
      expect(screen.getByText("Healthy")).toBeInTheDocument();
    });
    expect(screen.getByText("Bekleyen Is")).toBeInTheDocument();
    expect(screen.getByText("Islenen Is")).toBeInTheDocument();
    expect(screen.getByText("Basarisiz Is")).toBeInTheDocument();
    expect(screen.getByText("3")).toBeInTheDocument();
  });

  it("is listesi bosken EmptyState gosterilir", async () => {
    mockFetch(healthyEnvelope, emptyListEnvelope);

    renderOperationsPage();

    await waitFor(() => {
      expect(screen.getByText("Aktarim isi bulunamadi.")).toBeInTheDocument();
    });
  });
});
