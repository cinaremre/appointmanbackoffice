import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { ReactElement } from "react";
import { AuthProvider } from "../app/providers/AuthContext";
import { FinancePage } from "../features/finance/pages/FinancePage";

/**
 * FinancePage testleri (doküman Modül 16-17). Controller'dan çıkan şekil
 * birebir burada mock'lanır:
 * - GET /finance/sales  -> OkEnvelope: { data: { items, ..., totalGrandTotal } }
 * - GET /finance/payments -> OkList: { data: [...], pagination: {...} }
 */

/** ApiError'nin kullandigi Response sekli; jsdom'dan bagimsiz sade fake. */
function fakeResponse(body: unknown, status = 200) {
  return {
    ok: status >= 200 && status < 300,
    status,
    json: async () => body,
  } as unknown as Response;
}

/** Test sarmalayıcısı: query client + fake auth + router (URL query senkronu için). */
function renderWithProviders(ui: ReactElement, initialEntry = "/") {
  sessionStorage.setItem(
    "backoffice.auth",
    JSON.stringify({ token: "test-token", userId: "u-1", email: "admin@test.local", role: "SuperAdmin" }),
  );
  sessionStorage.setItem("backoffice.token", "test-token");

  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false, gcTime: 0 }, mutations: { retry: false } },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <MemoryRouter initialEntries={[initialEntry]}>{ui}</MemoryRouter>
      </AuthProvider>
    </QueryClientProvider>,
  );
}

const PAYMENT_ID = "3f2504e0-4f89-11d3-9a0c-0305e82c3301";
const BUSINESS_ID = "1f0e8a7c-2b3d-4e5f-9a8b-7c6d5e4f3a21";
const SALE_ID = "9b7a6c5d-4e3f-4a2b-8c1d-0e9f8a7b6c5d";

/** Controller sekliyle ödeme kaydı (AdminPaymentDto, camelCase). */
function paymentDto(amount: number) {
  return {
    id: PAYMENT_ID,
    businessId: BUSINESS_ID,
    saleId: SALE_ID,
    cashSessionId: null,
    amount,
    currency: "TRY",
    paymentMethod: 0,
    paidAt: "2026-11-01T10:30:00Z",
    status: 1,
    isRefund: false,
    refersToPaymentId: null,
  };
}

describe("FinancePage", () => {
  beforeEach(() => {
    // MUI Tabs jsdom'da scrollIntoView cagirabilir; guvenli stub.
    window.HTMLElement.prototype.scrollIntoView = () => {};

    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);

        // Satış ucu: OkEnvelope içinde liste + toplam tek nesne döner.
        if (url.includes("/finance/sales")) {
          return fakeResponse({
            data: {
              items: [],
              page: 1,
              pageSize: 50,
              totalCount: 0,
              totalPages: 0,
              hasMore: false,
              totalGrandTotal: 0,
            },
          });
        }

        // Ödeme ucu: OkList sekli (data + pagination).
        if (url.includes("/finance/payments")) {
          return fakeResponse({
            data: [paymentDto(1250.5)],
            pagination: { page: 1, pageSize: 50, totalCount: 1, totalPages: 1, hasMore: false },
          });
        }

        // Beklenmeyen çağrılar için boş zarf.
        return fakeResponse({ data: [] });
      }),
    );
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    sessionStorage.clear();
  });

  it("boş satış listesi EmptyState gösterir", async () => {
    renderWithProviders(<FinancePage />);

    // Aktif "Satışlar" sekmesi.
    expect(screen.getByRole("tab", { name: "Satışlar", selected: true })).toBeInTheDocument();

    // Boş liste: EmptyState etiketi tablo gövdesinde görünür.
    expect(await screen.findByText("Bu filtrelerle satış kaydı bulunamadı.")).toBeVisible();
  });

  it("dolu tahsilat listesinde tutar tr-TR biçiminde gösterilir", async () => {
    renderWithProviders(<FinancePage />, "/?tab=payments");

    expect(screen.getByRole("tab", { name: "Tahsilatlar", selected: true })).toBeInTheDocument();

    // Tutar hucreleri toLocaleString("tr-TR") + " TL" ile biçimlenir.
    const expectedAmount = `${(1250.5).toLocaleString("tr-TR")} TL`;
    expect(await screen.findByText(expectedAmount)).toBeVisible();
  });
});
