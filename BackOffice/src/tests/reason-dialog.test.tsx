import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { ReasonConfirmDialog } from "../shared/components/ReasonConfirmDialog";

/** Kritik işlem onayı: gerekçe olmadan işlem gönderilmez (doküman 5.4). */
describe("ReasonConfirmDialog", () => {
  it("gerekçe boşken onay butonu pasiftir", () => {
    render(
      <ReasonConfirmDialog
        open
        title="İşletmeyi Askya Al"
        impact="Mevcut randevular korunur."
        onCancel={() => {}}
        onConfirm={() => {}}
      />,
    );

    const confirmButton = screen.getByRole("button", { name: "Onayla" });
    expect(confirmButton).toBeDisabled();
  });

  it("gerekçe girilince onay çağrılır", async () => {
    const onConfirm = vi.fn();
    render(
      <ReasonConfirmDialog
        open
        title="İşletmeyi Askya Al"
        impact="Mevcut randevular korunur."
        onCancel={() => {}}
        onConfirm={onConfirm}
      />,
    );

    const reasonInput = screen.getByLabelText(/Gerekçe/i);
    fireEvent.change(reasonInput, { target: { value: "Şikayet incelemesi" } });

    await waitFor(() => {
      const confirmButton = screen.getByRole("button", { name: "Onayla" });
      expect(confirmButton).not.toBeDisabled();
    });

    fireEvent.click(screen.getByRole("button", { name: "Onayla" }));
    expect(onConfirm).toHaveBeenCalledWith("Şikayet incelemesi");
  });

  it("etki metni kullanıcıya açıkça gösterilir", () => {
    render(
      <ReasonConfirmDialog
        open
        title="İşletmeyi Askya Al"
        impact="Bu işletmeyi askıya almak yeni rezervasyonların görünürlüğünü etkileyebilir."
        onCancel={() => {}}
        onConfirm={() => {}}
      />,
    );

    expect(
      screen.getByText(/yeni rezervasyonların görünürlüğünü etkileyebilir/i),
    ).toBeInTheDocument();
  });
});
