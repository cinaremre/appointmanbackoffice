/**
 * Backoffice API tipleri. Sunucu envelope'u:
 * { data, pagination?, meta } — hatalar RFC 9457 ProblemDetails.
 */

export interface PaginationMeta {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasMore: boolean;
}

export interface AdminEnvelope<T> {
  data: T | null;
  pagination?: PaginationMeta;
  meta?: { correlationId?: string };
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}

/** Liste sorgularinda ortak sayfalama parametreleri (sunucu max 200 clamp'ler). */
export interface PageQuery {
  page?: number;
  pageSize?: number;
}

export function toQueryString(params: Record<string, unknown>): string {
  const parts: string[] = [];
  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null || value === "") continue;
    parts.push(`${encodeURIComponent(key)}=${encodeURIComponent(String(value))}`);
  }
  return parts.length ? `?${parts.join("&")}` : "";
}
