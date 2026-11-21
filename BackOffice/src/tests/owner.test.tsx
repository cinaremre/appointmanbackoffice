import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { ReactNode } from "react";
import { AuthProvider } from "../app/providers/AuthContext";
import { OwnerAppointmentsPage } from "../features/owner/pages/OwnerAppointmentsPage";
import { OwnerDashboardPage } from "../features/owner/pages/OwnerDashboardPage";

/**
 * Owner portal sayfa testleri. fetch mock URL farkindir: isletme listesi
 * uc'u 1 isletme doner (tek isletme otomatik secilir), dashboard uc'u
 * BusinessDashboardDto + DailySalesSummaryDto sekilli govde dondurur.
 * AuthProvider + QueryClientProvider + MemoryRouter sarmalayicilari
 * gercek sayfa ortamini taklit eder.
 */

const BUSINESSES = [
  {
    id: "b1",
    name: "Kuafor Merkez",
    description: "Sac ve sakal bakimi",
    isActive: true,
    rating: 4.2,
    totalReviews: 11,
    cityId: "c1",
    createdAt: "2026-01-02T09:00:00Z",
  },
];

const DASHBOARD = {
  dashboard: {
    businessId: "b1",
    date: "2026-11-22T00:00:00Z",
    bugunkiRandevular: 6,
    tamamlanan: 3,
    iptaller: 1,
    noShow: 1,
    dolulukOrani: 0.55,
  },
  summary: {
    date: "2026-11-22T00:00:00Z",
    businessId: "b1",
    salesCount: 3,
    totalSales: 1500,
    cashCollected: 900,
    cardCollected: 600,
    totalExpenses: 200,
    outstandingReceivables: 50,
    isCashOpen: true,
  },
};

/** Basarili liste cevabi: { data, pagination } envelope'u. */
function okEnvelope(data: unknown) {
  return {
    ok: true,
    status: 200,
    json: async () => ({
      data,
      pagination: {
        page: 1,
        pageSize: 50,
        totalCount: Array.isArray(data) ? data.length : 0,
        totalPages: 1,
        hasMore: false,
      },
    }),
  } as unknown as Response;
}

/** sessionStorage'a fake owner oturumu yazar (role: BusinessOwner). */
function fakeAuth() {
  sessionStorage.setItem(
    "backoffice.auth",
    JSON.stringify({
      token: "test-token",
      userId: "owner-1",
      email: "owner@example.com",
      role: "BusinessOwner",
    }),
  );
  sessionStorage.setItem("backoffice.token", "test-token");
}

/** URL'e gore cevap donderen fetch mock (owner cagrilari tam yol ister). */
function routeMock(handler: (url: string) => unknown) {
  vi.stubGlobal(
    "fetch",
    vi.fn(async (input: RequestInfo | URL) => okEnvelope(handler(String(input)))),
  );
}

function renderPage(page: ReactNode) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <MemoryRouter>{page}</MemoryRouter>
      </AuthProvider>
    </QueryClientProvider>,
  );
}

beforeEach(() => {
  fakeAuth();
});

afterEach(() => {
  sessionStorage.clear();
  vi.unstubAllGlobals();
});

describe("OwnerDashboardPage", () => {
  it("tek isletme otomatik secilir ve gunluk KPI basliklari gorunur", async () => {
    routeMock((url) => (url.includes("/dashboard") ? DASHBOARD : BUSINESSES));

    renderPage(<OwnerDashboardPage />);

    expect(await screen.findByText("Bugunki Randevular")).toBeInTheDocument();
    expect(screen.getByText("Gunluk Satis Ozeti")).toBeInTheDocument();
    expect(screen.getByText("Net Nakit Tahsilat")).toBeInTheDocument();
  });
});

describe("OwnerAppointmentsPage", () => {
  it("bos listede EmptyState gosterir", async () => {
    routeMock((url) => (url.includes("/appointments") ? [] : BUSINESSES));

    renderPage(<OwnerAppointmentsPage />);

    expect(await screen.findByText("Randevu bulunamadi.")).toBeInTheDocument();
  });
});
