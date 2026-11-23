/**
 * Kategori tipleri (doküman 10). Backend düz liste döner;
 * ağaç yapısını frontend parentId üzerinden kurar.
 */

/** GET /categories listesi öğeleri (düz, parentId içerir). */
export interface CategoryDto {
  id: string;
  name: string;
  parentId: string | null;
  displayOrder: number;
  isActive: boolean;
  childCount: number;
}

/** POST /categories gövdesi. */
export interface CategoryCreateRequest {
  name: string;
  parentId?: string;
  displayOrder?: number;
}

/** PATCH /categories/{id} gövdesi (kısmi güncelleme). */
export interface CategoryUpdateRequest {
  name?: string;
  displayOrder?: number;
  isActive?: boolean;
}

/** POST /categories/{id}/move gövdesi; targetParentId null ise kök seviyeye taşınır. */
export interface CategoryMoveRequest {
  targetParentId: string | null;
}

/** GET /categories/{id}/usage yanıtı: kullanım özeti. */
export interface CategoryUsageDto {
  categoryId: string;
  name: string;
  businessCount: number;
}

/** Frontend'in kurduğu ağaç düğümü: düz liste içinde derinlik bilgisi taşır. */
export interface CategoryNode extends CategoryDto {
  children: CategoryNode[];
  depth: number;
}
