/**
 * Servis tipleri (doküman 11). Liste sunucu tarafında sayfalanır;
 * toplu işlemler dryRun + execute iki fazlı çalışır.
 */

/** GET /services liste öğeleri. */
export interface ServiceDto {
  id: string;
  businessId: string;
  name: string;
  price: number;
  currency: string;
  durationMinutes: number;
  isActive: boolean;
}

/** PATCH /services/{id} gövdesi (kısmi güncelleme). */
export interface ServiceUpdateRequest {
  name?: string;
  price?: number;
  durationMinutes?: number;
}

/** Desteklenen toplu işlem türleri. */
export type BulkOperationType = "DeactivateServices" | "ActivateServices" | "ChangeServiceCategory";

/** POST /services/bulk-operations (dryRun: true) yanıtı. */
export interface BulkOperationPreviewDto {
  id: string;
  operationType: string;
  dryRun: boolean;
  status: string;
  totalRecords: number;
  invalidIds: string[];
}

/** POST /services/bulk-operations/{id}/execute yanıtı. */
export interface BulkOperationResultDto {
  id: string;
  status: string;
  successCount: number;
  failedCount: number;
  errorReport?: string | null;
}

/** POST /services/bulk-operations gövdesi. */
export interface BulkOperationCreateRequest {
  operation: BulkOperationType;
  serviceIds: string[];
  targetServiceCategoryId?: string;
  dryRun: boolean;
}
