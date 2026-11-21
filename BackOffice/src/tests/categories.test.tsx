import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { AuthProvider } from "../app/providers/AuthContext";
import { CategoriesPage } from "../features/categories/pages/CategoriesPage";

/**
 * CategoriesPage testleri (dokuman 10). Ortak sarmalayici:
 * AuthProvider + QueryClientProvider + MemoryRouter; oturum
 * sessionStorage fake'i ile kurulur (backoffice.auth + backoffice.token).
 */

function fakeAuth(): void {
  sessionStorage.setItem(
    "backoffice.auth",
    JSON.stringify({
      token: "test-token",
      userId: "user-1",
      email: "admin@test.test",
      role: "SuperAdmin",
    }),
  );
  sessionStorage.setItem("backoffice.token", "test-token");
}

/** client.ts'in kullandigi en kucuk Response benzeti. */
function jsonResponse(body: unknown): Response {
  return { ok: true, status: 200, json: async () => body } as unknown as Response;
}

function renderCategoriesPage(): ReturnType<typeof render> {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <MemoryRouter>
          <CategoriesPage />
        </MemoryRouter>
      </AuthProvider>
    </QueryClientProvider>,
  );
}

describe("CategoriesPage", () => {
  beforeEach(() => {
    sessionStorage.clear();
    fakeAuth();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    sessionStorage.clear();
  });

  it("kategorileri agac sirasiyla render eder ve istek token tasir", async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      jsonResponse({
        data: [
          { id: "cat-1", name: "Kuafor", parentId: null, displayOrder: 1, isActive: true, childCount: 1 },
          { id: "cat-2", name: "Sac Kesim", parentId: "cat-1", displayOrder: 1, isActive: true, childCount: 0 },
          { id: "cat-3", name: "Guzellik Salonu", parentId: null, displayOrder: 2, isActive: true, childCount: 0 },
        ],
      }),
    );
    vi.stubGlobal("fetch", fetchMock);

    renderCategoriesPage();

    // Agac render: parent ve cocuk isimleri gorunur.
    expect(await screen.findByText("Kuafor")).toBeInTheDocument();
    expect(screen.getByText("Sac Kesim")).toBeInTheDocument();
    expect(screen.getByText("Guzellik Salonu")).toBeInTheDocument();

    // Siralama: parent (sira 1) cocugundan once, root'lar displayOrder'a gore.
    const joined = (await screen.findAllByRole("listitem"))
      .map((item) => item.textContent ?? "")
      .join("|");
    expect(joined.indexOf("Kuafor")).toBeLessThan(joined.indexOf("Sac Kesim"));
    expect(joined.indexOf("Kuafor")).toBeLessThan(joined.indexOf("Guzellik Salonu"));

    // Istek Authorization header'i oturum token'i ile gitti.
    expect(fetchMock).toHaveBeenCalledWith(
      "/api/v1/admin/categories",
      expect.objectContaining({
        headers: expect.objectContaining({ Authorization: "Bearer test-token" }),
      }),
    );
  });

  it("liste bossa EmptyState gosterir", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(jsonResponse({ data: [] })));

    renderCategoriesPage();

    expect(await screen.findByText("Kategori bulunamadi.")).toBeInTheDocument();
  });
});
