// src/features/users/store/usersFilterStore.ts
import { create } from "zustand";
import { immer } from "zustand/middleware/immer";

interface UsersFilterState {
  search: string;
  role: string | null;
  status: "active" | "inactive" | "all";
  page: number;
  pageSize: number;
}

interface UsersFilterActions {
  setSearch: (search: string) => void;
  setRole: (role: string | null) => void;
  setStatus: (status: UsersFilterState["status"]) => void;
  setPage: (page: number) => void;
  resetFilters: () => void;
}

const initialFilters: UsersFilterState = {
  search: "",
  role: null,
  status: "all",
  page: 1,
  pageSize: 20,
};

export const useUsersFilterStore = create<
  UsersFilterState & UsersFilterActions
>()(
  immer((set) => ({
    ...initialFilters,

    setSearch: (search) =>
      set((state) => {
        state.search = search;
        state.page = 1; // Reset page on search change
      }),

    setRole: (role) =>
      set((state) => {
        state.role = role;
        state.page = 1;
      }),

    setStatus: (status) =>
      set((state) => {
        state.status = status;
        state.page = 1;
      }),

    setPage: (page) =>
      set((state) => {
        state.page = page;
      }),

    resetFilters: () => set(() => ({ ...initialFilters })),
  }))
);