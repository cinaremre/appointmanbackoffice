import type { AdminEnvelope, ProblemDetails } from "./types";

/**
 * Tek cekirdekli, tipli API istemcisi. Tekrarli elle HTTP servisleri
 * yerine tum cagrilar buradan gecer; envelope ve ProblemDetails
 * merkezî olarak cozulur (dokuman 23.1/23.3).
 */

export class ApiError extends Error {
  public status: number;
  public problem: ProblemDetails;

  constructor(status: number, problem: ProblemDetails) {
    super(problem.detail ?? problem.title ?? "Islem basarisiz.");
    this.name = "ApiError";
    this.status = status;
    this.problem = problem;
  }

  /** Kullaniciya gosterilecek kisa,Turkce mesaj; korelasyon destek icin. */
  get userMessage(): string {
    if (this.status === 401) return "Oturum suresi doldu veya yetkiniz yok. Lutfen tekrar giris yapin.";
    if (this.status === 403) return "Bu islem icin yetkiniz yok.";
    if (this.status === 404) return "Kayit bulunamadi.";
    if (this.status === 409) return this.problem.detail ?? "Islem gecerli durumla cakisiyor.";
    if (this.status === 429) return "Cok fazla istek gonderildi. Kisa bir sure bekleyin.";
    return this.problem.detail ?? this.problem.title ?? "Beklenmeyen bir hata olustu.";
  }

  get traceId(): string | undefined {
    return this.problem.traceId;
  }
}

const BASE_URL = "/api/v1/admin";

function readProblem(body: unknown, status: number): ProblemDetails {
  if (body && typeof body === "object") {
    return body as ProblemDetails;
  }
  return { status, title: "Bilinmeyen hata." };
}

async function parseBody(response: Response): Promise<unknown> {
  try {
    return await response.json();
  } catch {
    return null;
  }
}

export function authHeaders(): Record<string, string> {
  const token = sessionStorage.getItem("backoffice.token");
  return token ? { Authorization: `Bearer ${token}` } : {};
}

export async function request<T>(
  path: string,
  options: { method?: string; body?: unknown; signal?: AbortSignal } = {},
): Promise<AdminEnvelope<T>> {
  const method = options.method ?? "GET";
  const headers: Record<string, string> = {
    "Content-Type": "application/json",
    ...authHeaders(),
  };
  const body = options.body === undefined ? undefined : JSON.stringify(options.body);

  const response = await fetch(`${BASE_URL}${path}`, {
    method,
    headers,
    body,
    signal: options.signal,
  });

  const parsed = await parseBody(response);

  if (!response.ok) {
    throw new ApiError(response.status, readProblem(parsed, response.status));
  }

  return (parsed ?? { data: null }) as AdminEnvelope<T>;
}

/** Tekil kaynak: data dondurur. */
export async function getItem<T>(path: string, signal?: AbortSignal): Promise<T> {
  const envelope = await request<T>(path, { signal });
  return envelope.data as T;
}

/** Liste kaynagi: data + pagination dondurur. */
export async function getList<T>(
  path: string,
  signal?: AbortSignal,
): Promise<{ items: T[]; pagination: PaginationLike }> {
  const envelope = await request<T[]>(path, { signal });
  return {
    items: (envelope.data as T[]) ?? [],
    pagination: {
      page: envelope.pagination?.page ?? 1,
      pageSize: envelope.pagination?.pageSize ?? 50,
      totalCount: envelope.pagination?.totalCount ?? (envelope.data as T[])?.length ?? 0,
      totalPages: envelope.pagination?.totalPages ?? 1,
      hasMore: envelope.pagination?.hasMore ?? false,
    },
  };
}

export interface PaginationLike {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasMore: boolean;
}

/** POST mutation: iceriksiz basari durumunda data null olabilir. */
export async function postItem<T>(path: string, body?: unknown): Promise<T | null> {
  const envelope = await request<T>(path, { method: "POST", body });
  return envelope.data;
}

export async function patchItem<T>(path: string, body?: unknown): Promise<T | null> {
  const envelope = await request<T>(path, { method: "PATCH", body });
  return envelope.data;
}
