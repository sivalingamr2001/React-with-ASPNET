import { useDebounce } from "@/shared/hooks/useDebounce";
import { useUsersFilterStore } from "../store/usersFilterStore";
import { useUsers } from "./useUsers";

export const useUsersList = () => {
  const filters = useUsersFilterStore();
  const { data, isLoading, isError, error } = useUsers();
  const debouncedSearch = useDebounce(filters.search, 300);

  return {
    users: data?.data ?? [],
    pagination: data?.meta,
    isLoading,
    isError,
    error,
    filters,
    debouncedSearch,
  } as const;
};