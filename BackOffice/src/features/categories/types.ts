/**
 * Kategori tipleri (dokuman 10). Backend duz liste doner;
 * agac yapisini frontend parentId uzerinden kurar.
 */

/** GET /categories listesi ogeleri (duz, parentId icerir). */
export interface CategoryDto {
  id: string;
  name: string;
  parentId: string | null;
  displayOrder: number;
  isActive: boolean;
  childCount: number;
}

/** POST /categories govdesi. */
export interface CategoryCreateRequest {
  name: string;
  parentId?: string;
  displayOrder?: number;
}

/** PATCH /categories/{id} govdesi (kismi guncelleme). */
export interface CategoryUpdateRequest {
  name?: string;
  displayOrder?: number;
  isActive?: boolean;
}

/** POST /categories/{id}/move govdesi; targetParentId null ise kok seviyeye tasinir. */
export interface CategoryMoveRequest {
  targetParentId: string | null;
}

/** GET /categories/{id}/usage yaniti: kullanim ozeti. */
export interface CategoryUsageDto {
  categoryId: string;
  name: string;
  businessCount: number;
}

/** Frontend'in kurdugu agac dugumu: duz liste icinde derinlik bilgisi tasir. */
export interface CategoryNode extends CategoryDto {
  children: CategoryNode[];
  depth: number;
}
