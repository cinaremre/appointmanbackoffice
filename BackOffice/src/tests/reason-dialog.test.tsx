import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { ReasonConfirmDialog } from "../shared/components/ReasonConfirmDialog";

/** Kritik islem onayi: gerekce olmadan islem gonderilmez (dokuman 5.4). */
describe("ReasonConfirmDialog", () => {
  it("gerekce bosken onay butonu pasiftir", () => {
    render(
      <ReasonConfirmDialog
        open
        title="Isletmeyi Askya Al"
        impact="Mevcut randevular korunur."
        onCancel={() => {}}
        onConfirm={() => {}}
      />,
    );

    const confirmButton = screen.getByRole("button", { name: "Onayla" });
    expect(confirmButton).toBeDisabled();
  });

  it("gerekce girilince onay cagrilir", async () => {
    const onConfirm = vi.fn();
    render(
      <ReasonConfirmDialog
        open
        title="Isletmeyi Askya Al"
        impact="Mevcut randevular korunur."
        onCancel={() => {}}
        onConfirm={onConfirm}
      />,
    );

    const reasonInput = screen.getByLabelText(/Gerekce/i);
    fireEvent.change(reasonInput, { target: { value: "Sikayet incelemesi" } });

    await waitFor(() => {
      const confirmButton = screen.getByRole("button", { name: "Onayla" });
      expect(confirmButton).not.toBeDisabled();
    });

    fireEvent.click(screen.getByRole("button", { name: "Onayla" }));
    expect(onConfirm).toHaveBeenCalledWith("Sikayet incelemesi");
  });

  it("etki metni kullanicaya acikca gosterilir", () => {
    render(
      <ReasonConfirmDialog
        open
        title="Isletmeyi Askya Al"
        impact="Bu isletmeyi askiya almak yeni rezervasyonlarin gorunurlugunu etkileyebilir."
        onCancel={() => {}}
        onConfirm={() => {}}
      />,
    );

    expect(
      screen.getByText(/yeni rezervasyonlarin gorunurlugunu etkileyebilir/i),
    ).toBeInTheDocument();
  });
});
