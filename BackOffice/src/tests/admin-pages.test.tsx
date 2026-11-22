import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import { AuthProvider } from "../app/providers/AuthContext";
import { ServicesPage } from "../features/services/pages/ServicesPage";
import { AppointmentsPage } from "../features/appointments/pages/AppointmentsPage";
import { SubscriptionsPage } from "../features/subscriptions/pages/SubscriptionsPage";

/**
 * Admin sayfalarinin smoke testleri: sahte oturum + fetch mock ile
 * sayfanin veriyi render ettigi dogrulanir (dokuman 28.2 component tests).
 */

function seedAuth(role = "SuperAdmin") {
  sessionStorage.setItem(
    "backoffice.auth",
    JSON.stringify({ token: "t", userId: "u", email: "e@x.com", role }),
  );
  sessionStorage.setItem("backoffice.token", "t");
}

function renderPage(ui: React.ReactNode) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={client}>
      <AuthProvider>
        <MemoryRouter>{ui}</MemoryRouter>
      </AuthProvider>
    </QueryClientProvider>,
  );
}

function jsonEnvelope(data: unknown, pagination?: unknown) {
  return { data, pagination: pagination ?? null, meta: { correlationId: "t" } };
}

describe("Admin sayfalari (smoke)", () => {
  afterEach(() => {
    sessionStorage.clear();
    vi.restoreAllMocks();
  });

  it("ServicesPage servis listesini render eder", async () => {
    seedAuth();
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(
        new Response(
          JSON.stringify(
            jsonEnvelope(
              [
                { id: "s1", businessId: "b1", name: "Sac Kesimi", price: 500, currency: "TRY", durationMinutes: 30, isActive: true },
              ],
              { page: 1, pageSize: 50, totalCount: 1, totalPages: 1, hasMore: false },
            ),
          ),
          { status: 200 },
        ),
      ),
    );

    renderPage(<ServicesPage />);

    expect(await screen.findByText("Sac Kesimi")).toBeInTheDocument();
  });

  it("AppointmentsPage durum etiketini Turkce gosterir", async () => {
    seedAuth();
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(
        new Response(
          JSON.stringify(
            jsonEnvelope(
              [
                { id: "a1", businessId: "b1", userId: "u1", employeeId: "e1", serviceId: "s1", appointmentDate: "2026-11-25T14:00:00Z", status: 1, notes: null },
              ],
              { page: 1, pageSize: 50, totalCount: 1, totalPages: 1, hasMore: false },
            ),
          ),
          { status: 200 },
        ),
      ),
    );

    renderPage(<AppointmentsPage />);

    // status 1 -> Confirmed; sayisal kod ham gosterilmemeli.
    expect(await screen.findByText("Confirmed")).toBeInTheDocument();
    expect(screen.queryByText(/^1$/)).not.toBeInTheDocument();
  });

  it("SubscriptionsPage bos plan listesi icin EmptyState gosterir", async () => {
    seedAuth();
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(
        new Response(JSON.stringify(jsonEnvelope([])), { status: 200 }),
      ),
    );

    renderPage(<SubscriptionsPage />);

    expect(await screen.findByText(/Henuz abonelik plani tanimlanmamis/i)).toBeInTheDocument();
  });
});
