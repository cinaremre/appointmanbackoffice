import type { AdminEnvelope, ProblemDetails } from "./types";

/**
 * Tek çekirdekli, tipli API istemcisi. Tekrarlı elle HTTP servisleri
 * yerine tüm çağrılar buradan geçer; envelope ve ProblemDetails
 * merkezî olarak çözülür (doküman 23.1/23.3).
 */

export class ApiError extends Error {
  public status: number;
  public problem: ProblemDetails;

  constructor(status: number, problem: ProblemDetails) {
    super(problem.detail ?? problem.title ?? "İşlem başarısız.");
    this.name = "ApiError";
    this.status = status;
    this.problem = problem;
  }

  /** Kullanıcıya gösterilecek kısa,Türkçe mesaj; korelasyon destek için. */
  get userMessage(): string {
    if (this.status === 401) return "Oturum süresi doldu veya yetkiniz yok. Lütfen tekrar giriş yapın.";
    if (this.status === 403) return "Bu işlem için yetkiniz yok.";
    if (this.status === 404) return "Kayıt bulunamadı.";
    if (this.status === 409) return this.problem.detail ?? "İşlem geçerli durumla çakışıyor.";
    if (this.status === 429) return "Çok fazla istek gönderildi. Kısa bir süre bekleyin.";
    return this.problem.detail ?? this.problem.title ?? "Beklenmeyen bir hata oluştu.";
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

/** Tekil kaynak: data döndürür. */
export async function getItem<T>(path: string, signal?: AbortSignal): Promise<T> {
  const envelope = await request<T>(path, { signal });
  return envelope.data as T;
}

/** Liste kaynağı: data + pagination döndürür. */
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

/** POST mutation: iceriksiz başarı durumunda data null olabilir. */
export async function postItem<T>(path: string, body?: unknown): Promise<T | null> {
  const envelope = await request<T>(path, { method: "POST", body });
  return envelope.data;
}

export async function patchItem<T>(path: string, body?: unknown): Promise<T | null> {
  const envelope = await request<T>(path, { method: "PATCH", body });
  return envelope.data;
}
