import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { AuthProvider } from "../app/providers/AuthContext";
import { BusinessListPage } from "../features/businesses/pages/BusinessListPage";
import type { AdminEnvelope } from "../shared/api/types";

/**
 * İŞLETME liste sayfası testleri (doküman 9).
 * Sayfa AuthProvider + QueryClientProvider + MemoryRouter ile sarmalanır;
 * oturum sessionStorage'daki sahte auth ile kurulur, fetch mock'lanır.
 */

const BUSINESS_ITEM = {
  id: "b-1",
  name: "Kuaför Altın Makas",
  status: "Active",
  cityId: "34",
  ownerId: "o-1",
  createdAt: "2026-01-15T10:00:00Z",
  rating: 4.7,
  totalReviews: 128,
};

function envelope(items: unknown[], totalCount: number): AdminEnvelope<unknown[]> {
  return {
    data: items,
    pagination: { page: 1, pageSize: 50, totalCount, totalPages: 1, hasMore: false },
  };
}

/** client'in bekledigi minimal Response taklidi (ok + json). */
function jsonResponse(body: unknown): Response {
  return {
    ok: true,
    status: 200,
    json: async () => body,
  } as unknown as Response;
}

/** Login akışını atlamak için sahte oturum; AuthContext sessionStorage'dan okur. */
function seedSession(): void {
  sessionStorage.setItem(
    "backoffice.auth",
    JSON.stringify({ token: "t", userId: "u", email: "e@x.com", role: "SuperAdmin" }),
  );
  sessionStorage.setItem("backoffice.token", "t");
}

function renderListPage(): void {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  render(
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <MemoryRouter>
          <BusinessListPage />
        </MemoryRouter>
      </AuthProvider>
    </QueryClientProvider>,
  );
}

describe("BusinessListPage", () => {
  beforeEach(() => {
    seedSession();
  });

  afterEach(() => {
    cleanup();
    sessionStorage.clear();
    vi.unstubAllGlobals();
  });

  it("işletme listesini yükler ve işletme adını gösterir", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(jsonResponse(envelope([BUSINESS_ITEM], 1))));

    renderListPage();

    expect(await screen.findByText("Kuaför Altın Makas")).toBeVisible();
  });

  it("boş listede boş durum mesajı görünür", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(jsonResponse(envelope([], 0))));

    renderListPage();

    expect(await screen.findByText("Filtreye uygun işletme bulunamadı.")).toBeVisible();
  });

  it("onay bekleyen işletmede Onayla aksiyonu görünür", async () => {
    const pending = { ...BUSINESS_ITEM, id: "b-2", status: "PendingApproval" };
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(jsonResponse(envelope([pending], 1))));

    renderListPage();

    expect(await screen.findByRole("button", { name: "Onayla" })).toBeVisible();
  });
});
