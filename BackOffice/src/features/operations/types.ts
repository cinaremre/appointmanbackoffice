/**
 * Operasyon izleme tipleri (doküman Modül 20). Backend enum'ları sayı olarak
 * serileştirilir; EnumImportStatus / EnumImportRowStatus ile eşleşir.
 */

/** EnumImportStatus: veri aktarım isinin durumu. */
export type ImportStatus = 0 | 1 | 2 | 3 | 4 | 5;

export const IMPORT_STATUS_LABELS: Record<number, string> = {
  0: "Yüklendi",
  1: "Eslesmede",
  2: "Onaylandı",
  3: "İşleniyor",
  4: "Tamamlandı",
  5: "Başarısız",
};

/** Durum filtresi select'i için seçenek listesi. */
export const IMPORT_STATUS_OPTIONS: Array<{ value: ImportStatus; label: string }> = [
  { value: 0, label: "Yüklendi" },
  { value: 1, label: "Eslesmede" },
  { value: 2, label: "Onaylandı" },
  { value: 3, label: "İşleniyor" },
  { value: 4, label: "Tamamlandı" },
  { value: 5, label: "Başarısız" },
];

/** EnumImportRowStatus: tek bir aktarım satırının sonucu. */
export type ImportRowStatus = 0 | 1 | 2 | 3;

export const IMPORT_ROW_STATUS_LABELS: Record<number, string> = {
  0: "Beklemede",
  1: "Aktarildi",
  2: "Mukerrer Atlandi",
  3: "Başarısız",
};

/** Aktarım işi (AdminJobDto). */
export interface ImportJob {
  id: string;
  businessId: string;
  sourceSystem: string;
  status: number;
  totalRows: number;
  importedRows: number;
  failedRows: number;
  startedAt?: string | null;
  completedAt?: string | null;
}

/** Aktarım satır sonucu (AdminJobRowDto). */
export interface ImportJobRow {
  id: string;
  rowNumber: number;
  sourceRecordKey?: string | null;
  status: number;
  errorDetails?: string | null;
}

/** İş detayı (AdminJobDetailDto): iş özeti + satır sonuçlarının ilk 200'ü. */
export interface ImportJobDetail {
  job: ImportJob;
  rows: ImportJobRow[];
}

/** Başarısız satır ornegi (AdminFailedRowDto). */
export interface FailedRowSample {
  importJobId: string;
  rowNumber: number;
  sourceRecordKey?: string | null;
  errorDetails?: string | null;
}

/** Başarısız operasyon raporu (AdminFailedOperationsDto). */
export interface FailedOperations {
  failedJobs: ImportJob[];
  failedRows: FailedRowSample[];
}

/** Sistem sağlık raporu (SystemHealthDto). */
export interface SystemHealth {
  status: string;
  pendingJobs: number;
  runningJobs: number;
  failedJobs: number;
  note: string;
}
