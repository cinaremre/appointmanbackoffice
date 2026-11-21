/**
 * Servis tipleri (dokuman 11). Liste sunucu tarafinda sayfalanir;
 * toplu islemler dryRun + execute iki fazli calisir.
 */

/** GET /services liste ogeleri. */
export interface ServiceDto {
  id: string;
  businessId: string;
  name: string;
  price: number;
  currency: string;
  durationMinutes: number;
  isActive: boolean;
}

/** PATCH /services/{id} govdesi (kismi guncelleme). */
export interface ServiceUpdateRequest {
  name?: string;
  price?: number;
  durationMinutes?: number;
}

/** Desteklenen toplu islem turleri. */
export type BulkOperationType = "DeactivateServices" | "ActivateServices" | "ChangeServiceCategory";

/** POST /services/bulk-operations (dryRun: true) yaniti. */
export interface BulkOperationPreviewDto {
  id: string;
  operationType: string;
  dryRun: boolean;
  status: string;
  totalRecords: number;
  invalidIds: string[];
}

/** POST /services/bulk-operations/{id}/execute yaniti. */
export interface BulkOperationResultDto {
  id: string;
  status: string;
  successCount: number;
  failedCount: number;
  errorReport?: string | null;
}

/** POST /services/bulk-operations govdesi. */
export interface BulkOperationCreateRequest {
  operation: BulkOperationType;
  serviceIds: string[];
  targetServiceCategoryId?: string;
  dryRun: boolean;
}
