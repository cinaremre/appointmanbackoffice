import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError, getItem, getList, postItem, request } from "../shared/api/client";
import { toQueryString } from "../shared/api/types";

/** Merkezi API istemcisi: envelope çözümleme ve ProblemDetails eşleme. */
describe("api client", () => {
  const originalFetch = globalThis.fetch;

  beforeEach(() => {
    sessionStorage.setItem("backoffice.token", "test-token");
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
    sessionStorage.clear();
    vi.restoreAllMocks();
  });

  it("token'i Authorization header'ina taşır", async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ data: { id: "1" } }), { status: 200 }),
    );
    globalThis.fetch = fetchMock as unknown as typeof fetch;

    await getItem("/health-check");

    const init = fetchMock.mock.calls[0][1] as RequestInit;
    expect((init.headers as Record<string, string>).Authorization).toBe("Bearer test-token");
  });

  it("ProblemDetails'i ApiError'a çevirir", async () => {
    globalThis.fetch = vi.fn().mockResolvedValue(
      new Response(
        JSON.stringify({ type: "x", title: "İşlem başarısız.", status: 409, detail: "Çakışma." }),
        { status: 409 },
      ),
    ) as unknown as typeof fetch;

    await expect(getItem("/businesses/1")).rejects.toBeInstanceOf(ApiError);
    await expect(getItem("/businesses/1")).rejects.toMatchObject({
      status: 409,
    });
  });

  it("409 kullanıcıya çakışma mesajı gösterir", () => {
    const error = new ApiError(409, { detail: "Bu işletme zaten askıda." });
    expect(error.userMessage).toContain("askıda");
  });

  it("401 oturum mesajı döndürür", () => {
    const error = new ApiError(401, { title: "Unauthorized" });
    expect(error.userMessage).toContain("giriş");
  });

  it("liste cevabından items + pagination çözer", async () => {
    globalThis.fetch = vi.fn().mockResolvedValue(
      new Response(
        JSON.stringify({
          data: [{ id: "a" }, { id: "b" }],
          pagination: { page: 1, pageSize: 50, totalCount: 2, totalPages: 1, hasMore: false },
        }),
        { status: 200 },
      ),
    ) as unknown as typeof fetch;

    const result = await getList("/businesses");
    expect(result.items).toHaveLength(2);
    expect(result.pagination.totalCount).toBe(2);
  });

  it("POST govdeyi JSON olarak gönderir", async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ data: { id: "1" } }), { status: 200 }),
    );
    globalThis.fetch = fetchMock as unknown as typeof fetch;

    await postItem("/businesses/1/suspend", { reason: "test gerekçe" });

    const init = fetchMock.mock.calls[0][1] as RequestInit;
    expect(init.method).toBe("POST");
    expect(JSON.parse(String(init.body))).toEqual({ reason: "test gerekçe" });
  });

  it("oturumsuz istek Authorization göndermez", async () => {
    sessionStorage.removeItem("backoffice.token");
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ data: null }), { status: 200 }),
    );
    globalThis.fetch = fetchMock as unknown as typeof fetch;

    await request("/reports/platform-overview");

    const init = fetchMock.mock.calls[0][1] as RequestInit;
    expect((init.headers as Record<string, string>).Authorization).toBeUndefined();
  });
});

describe("toQueryString", () => {
  it("boş ve tanımsız değerleri atlar", () => {
    expect(toQueryString({ status: "Active", search: undefined, page: 0 })).toBe(
      "?status=Active&page=0",
    );
  });

  it("tamamen boşken boş metin döndürür", () => {
    expect(toQueryString({})).toBe("");
  });
});

