/**
 * Operasyon izleme tipleri (dokuman Modul 20). Backend enum'lari sayi olarak
 * serilestirilir; EnumImportStatus / EnumImportRowStatus ile eslesir.
 */

/** EnumImportStatus: veri aktarim isinin durumu. */
export type ImportStatus = 0 | 1 | 2 | 3 | 4 | 5;

export const IMPORT_STATUS_LABELS: Record<number, string> = {
  0: "Yuklendi",
  1: "Eslesmede",
  2: "Onaylandi",
  3: "Isleniyor",
  4: "Tamamlandi",
  5: "Basarisiz",
};

/** Durum filtresi select'i icin secenek listesi. */
export const IMPORT_STATUS_OPTIONS: Array<{ value: ImportStatus; label: string }> = [
  { value: 0, label: "Yuklendi" },
  { value: 1, label: "Eslesmede" },
  { value: 2, label: "Onaylandi" },
  { value: 3, label: "Isleniyor" },
  { value: 4, label: "Tamamlandi" },
  { value: 5, label: "Basarisiz" },
];

/** EnumImportRowStatus: tek bir aktarim satirinin sonucu. */
export type ImportRowStatus = 0 | 1 | 2 | 3;

export const IMPORT_ROW_STATUS_LABELS: Record<number, string> = {
  0: "Beklemede",
  1: "Aktarildi",
  2: "Mukerrer Atlandi",
  3: "Basarisiz",
};

/** Aktarim isi (AdminJobDto). */
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

/** Aktarim satir sonucu (AdminJobRowDto). */
export interface ImportJobRow {
  id: string;
  rowNumber: number;
  sourceRecordKey?: string | null;
  status: number;
  errorDetails?: string | null;
}

/** Is detayi (AdminJobDetailDto): is ozeti + satir sonuclarinin ilk 200'u. */
export interface ImportJobDetail {
  job: ImportJob;
  rows: ImportJobRow[];
}

/** Basarisiz satir ornegi (AdminFailedRowDto). */
export interface FailedRowSample {
  importJobId: string;
  rowNumber: number;
  sourceRecordKey?: string | null;
  errorDetails?: string | null;
}

/** Basarisiz operasyon raporu (AdminFailedOperationsDto). */
export interface FailedOperations {
  failedJobs: ImportJob[];
  failedRows: FailedRowSample[];
}

/** Sistem saglik raporu (SystemHealthDto). */
export interface SystemHealth {
  status: string;
  pendingJobs: number;
  runningJobs: number;
  failedJobs: number;
  note: string;
}
