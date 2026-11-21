import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { AuthProvider } from "../app/providers/AuthContext";
import { UserListPage } from "../features/users/pages/UserListPage";

/**
 * Kullanici listesi sayfa testleri (dokuman 12). fetch mock'lanir;
 * AuthProvider + QueryClientProvider + MemoryRouter sarmalayicilari
 * gercek sayfa ortamini taklit eder.
 */

const USERS = [
  {
    id: "u1",
    firstName: "Ali",
    lastName: "Yilmaz",
    email: "ali@example.com",
    phoneNumber: "5551112233",
    role: "Admin",
    isLockedOut: false,
    createdAt: "2026-01-05T10:00:00Z",
  },
  {
    id: "u2",
    firstName: "Ayse",
    lastName: "Demir",
    email: "ayse@example.com",
    phoneNumber: "5554445566",
    role: "BusinessOwner",
    isLockedOut: true,
    createdAt: "2026-02-11T09:30:00Z",
  },
];

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

/** sessionStorage'a fake backoffice oturumu yazar (role: PlatformAdmin). */
function fakeAuth() {
  sessionStorage.setItem(
    "backoffice.auth",
    JSON.stringify({ token: "test-token", userId: "admin-1", email: "admin@example.com", role: "PlatformAdmin" }),
  );
  sessionStorage.setItem("backoffice.token", "test-token");
}

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <MemoryRouter>
          <UserListPage />
        </MemoryRouter>
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

describe("UserListPage", () => {
  it("kullanici listesini render eder ve e-postalar gorunur", async () => {
    vi.stubGlobal("fetch", vi.fn(async () => okEnvelope(USERS)));

    renderPage();

    expect(await screen.findByText("ali@example.com")).toBeInTheDocument();
    expect(screen.getByText("ayse@example.com")).toBeInTheDocument();
  });

  it("bos listede EmptyState gosterir", async () => {
    vi.stubGlobal("fetch", vi.fn(async () => okEnvelope([])));

    renderPage();

    expect(await screen.findByText("Kayitli kullanici bulunamadi.")).toBeInTheDocument();
    expect(screen.queryByText("ali@example.com")).not.toBeInTheDocument();
  });
});
