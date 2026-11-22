// BackOffice API'nin Swagger dokumanindan TypeScript tiplerini uretir.
// DTO alan adlari backend ile elle senkronize edilmemeli; bu script
// `openapi-typescript` ile schema.d.ts uretir ve kaynak tek gercek olur.
//
// Kullanim (API ayakta iken, ornek: http://localhost:5099):
//   npm run generate:types -- http://localhost:5099/swagger/v1/swagger.json
//
// Uretilen dosya: src/shared/api/generated/schema.d.ts
// Bu dosya commit edilir; backend DTO degisince script yeniden calistirilir.

import { execSync } from "node:child_process";
import { mkdirSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));

// Swagger URL argumandan ya da varsayilan BackOffice API adresinden gelir.
const swaggerUrl = process.argv[2] ?? "http://localhost:5099/swagger/v1/swagger.json";
const outFile = resolve(__dirname, "../src/shared/api/generated/schema.d.ts");

mkdirSync(dirname(outFile), { recursive: true });

console.log(`OpenAPI tipleri uretiliyor: ${swaggerUrl}`);
execSync(`npx openapi-typescript "${swaggerUrl}" -o "${outFile}"`, {
  stdio: "inherit",
  cwd: resolve(__dirname, ".."),
});

console.log(`Uretildi: ${outFile}`);
