import { useQuery } from "@tanstack/react-query";
import { ApiError, authHeaders } from "../../../shared/api/client";
import type { AdminEnvelope, ProblemDetails } from "../../../shared/api/types";
import type { OwnedBusinessListItem } from "../types";

/**
 * Owner portal API istemcisi. Backoffice client'inin BASE_URL sabiti
 * "/api/v1/admin" olduğundan owner uçları (tam yol "/api/v1/owner/...")
 * client.ts üzerinden CAGRILMAZ; burada aynı envelope/ApiError/problem
 * desenini kullanan ayrı, küçük bir fetch sarmalayıcı tanımlanır
 * (doküman 23.1/23.3). Token yine sessionStorage'dan authHeaders()
 * ile okunur; hatalarda aynı ApiError firlatilir.
 */

const OWNER_BASE_URL = "/api/v1/owner";

/** Liste sonucunda döndürülen sayfalama bilgisi (client PaginationLike eşdeğeri). */
export interface OwnerPagination {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasMore: boolean;
}

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

/** Tam yol owner isteği: envelope çözümlenir, hatalarda ApiError atilir. */
export async function ownerRequest<T>(
  path: string,
  options: { method?: string; body?: unknown; signal?: AbortSignal } = {},
): Promise<AdminEnvelope<T>> {
  const method = options.method ?? "GET";
  const headers: Record<string, string> = {
    "Content-Type": "application/json",
    ...authHeaders(),
  };
  const body = options.body === undefined ? undefined : JSON.stringify(options.body);

  const response = await fetch(`${OWNER_BASE_URL}${path}`, {
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

/** Tekil owner kaynağı: data alanını döndürür. */
export async function ownerGetItem<T>(path: string, signal?: AbortSignal): Promise<T> {
  const envelope = await ownerRequest<T>(path, { signal });
  return envelope.data as T;
}

/** Liste owner kaynağı: pagination yoksa toleranslı varsayılan üretir. */
export async function ownerGetList<T>(
  path: string,
  signal?: AbortSignal,
): Promise<{ items: T[]; pagination: OwnerPagination }> {
  const envelope = await ownerRequest<T[]>(path, { signal });
  const items = (envelope.data as T[] | null) ?? [];
  return {
    items,
    pagination: {
      page: envelope.pagination?.page ?? 1,
      pageSize: envelope.pagination?.pageSize ?? 50,
      totalCount: envelope.pagination?.totalCount ?? items.length,
      totalPages: envelope.pagination?.totalPages ?? 1,
      hasMore: envelope.pagination?.hasMore ?? false,
    },
  };
}

/** Owner POST mutation: iceriksiz basarida data null olabilir. */
export async function ownerPostItem<T>(path: string, body?: unknown): Promise<T | null> {
  const envelope = await ownerRequest<T>(path, { method: "POST", body });
  return envelope.data;
}

/** Owner PATCH mutation. */
export async function ownerPatchItem<T>(path: string, body?: unknown): Promise<T | null> {
  const envelope = await ownerRequest<T>(path, { method: "PATCH", body });
  return envelope.data;
}

/**
 * Owner'in işletmeleri: tüm owner sayfalarındaki işletme seçiminin tek
 * kaynağı. Tek işletme varsa sayfalar otomatik olarak onu secer.
 */
export function useOwnedBusinesses() {
  return useQuery({
    queryKey: ["owner", "businesses", "list"],
    queryFn: ({ signal }) => ownerGetList<OwnedBusinessListItem>("/businesses", signal),
  });
}
