import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError, getItem, getList, postItem, request } from "../shared/api/client";
import { toQueryString } from "../shared/api/types";

/** Merkezi API istemcisi: envelope cozumleme ve ProblemDetails esleme. */
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

  it("token'i Authorization header'ina tasir", async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ data: { id: "1" } }), { status: 200 }),
    );
    globalThis.fetch = fetchMock as unknown as typeof fetch;

    await getItem("/health-check");

    const init = fetchMock.mock.calls[0][1] as RequestInit;
    expect((init.headers as Record<string, string>).Authorization).toBe("Bearer test-token");
  });

  it("ProblemDetails'i ApiError'a cevirir", async () => {
    globalThis.fetch = vi.fn().mockResolvedValue(
      new Response(
        JSON.stringify({ type: "x", title: "Islem basarisiz.", status: 409, detail: "Cakisma." }),
        { status: 409 },
      ),
    ) as unknown as typeof fetch;

    await expect(getItem("/businesses/1")).rejects.toBeInstanceOf(ApiError);
    await expect(getItem("/businesses/1")).rejects.toMatchObject({
      status: 409,
    });
  });

  it("409 kullanicuya cakisma mesaji gosterir", () => {
    const error = new ApiError(409, { detail: "Bu isletme zaten askida." });
    expect(error.userMessage).toContain("askida");
  });

  it("401 oturum mesaji dondurur", () => {
    const error = new ApiError(401, { title: "Unauthorized" });
    expect(error.userMessage).toContain("giris");
  });

  it("liste cevabindan items + pagination cozer", async () => {
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

  it("POST govdeyi JSON olarak gonderir", async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ data: { id: "1" } }), { status: 200 }),
    );
    globalThis.fetch = fetchMock as unknown as typeof fetch;

    await postItem("/businesses/1/suspend", { reason: "test gerekce" });

    const init = fetchMock.mock.calls[0][1] as RequestInit;
    expect(init.method).toBe("POST");
    expect(JSON.parse(String(init.body))).toEqual({ reason: "test gerekce" });
  });

  it("oturumsuz istek Authorization gondermez", async () => {
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
  it("bos ve tanimsiz degerleri atlar", () => {
    expect(toQueryString({ status: "Active", search: undefined, page: 0 })).toBe(
      "?status=Active&page=0",
    );
  });

  it("tamamen bosken bos metin dondurur", () => {
    expect(toQueryString({})).toBe("");
  });
});

