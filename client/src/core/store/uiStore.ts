import { create } from "zustand";
import { devtools } from "zustand/middleware";
import { immer } from "zustand/middleware/immer";

interface UIState {
  sidebarOpen: boolean;
  globalLoading: boolean;
  activeModal: string | null;
}

interface UIActions {
  toggleSidebar: () => void;
  setSidebarOpen: (open: boolean) => void;
  setGlobalLoading: (loading: boolean) => void;
  openModal: (modalId: string) => void;
  closeModal: () => void;
}

type UIStore = UIState & UIActions;

export const useUIStore = create<UIStore>()(
  devtools(
    immer((set) => ({
      sidebarOpen: true,
      globalLoading: false,
      activeModal: null,

      toggleSidebar: () =>
        set((state) => {
          state.sidebarOpen = !state.sidebarOpen;
        }),

      setSidebarOpen: (open) =>
        set((state) => {
          state.sidebarOpen = open;
        }),

      setGlobalLoading: (loading) =>
        set((state) => {
          state.globalLoading = loading;
        }),

      openModal: (modalId) =>
        set((state) => {
          state.activeModal = modalId;
        }),

      closeModal: () =>
        set((state) => {
          state.activeModal = null;
        }),
    })),
    { name: "UIStore" }
  )
);