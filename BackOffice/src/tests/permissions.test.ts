import { describe, expect, it } from "vitest";
import { hasPermission, PERMISSIONS, ROLE_PERMISSIONS } from "../shared/constants/permissions";

/** Dokuman 5.2/5.3: rol -> permission eslemesinin frontend kopyasi. */
describe("hasPermission", () => {
  it("SuperAdmin her izne sahiptir", () => {
    expect(hasPermission(["SuperAdmin"], PERMISSIONS.auditRead)).toBe(true);
    expect(hasPermission(["SuperAdmin"], PERMISSIONS.financeRead)).toBe(true);
  });

  it("ReadOnlyAnalyst yalnizca rapor/okuma izinlerine sahiptir", () => {
    expect(hasPermission(["ReadOnlyAnalyst"], PERMISSIONS.reportRead)).toBe(true);
    expect(hasPermission(["ReadOnlyAnalyst"], PERMISSIONS.businessRead)).toBe(true);
    expect(hasPermission(["ReadOnlyAnalyst"], PERMISSIONS.businessSuspend)).toBe(false);
    expect(hasPermission(["ReadOnlyAnalyst"], PERMISSIONS.userManage)).toBe(false);
  });

  it("SupportAgent sinirli operasyon izni tasir", () => {
    expect(hasPermission(["SupportAgent"], PERMISSIONS.appointmentCancel)).toBe(true);
    expect(hasPermission(["SupportAgent"], PERMISSIONS.financeRead)).toBe(false);
    expect(hasPermission(["SupportAgent"], PERMISSIONS.featureOverride)).toBe(false);
  });

  it("bilinmeyen rollerin izni yoktur", () => {
    expect(hasPermission(["Customer"], PERMISSIONS.reportRead)).toBe(false);
    expect(hasPermission([], PERMISSIONS.reportRead)).toBe(false);
  });

  it("matriste tanimli her izin en az bir role baglidir", () => {
    const allMatrixPermissions = new Set(
      Object.values(ROLE_PERMISSIONS).flatMap((permissions) => permissions as string[]),
    );
    for (const permission of Object.values(PERMISSIONS)) {
      expect(allMatrixPermissions.has(permission)).toBe(true);
    }
  });
});
