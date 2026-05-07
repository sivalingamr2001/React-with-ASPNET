import { useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/core/query/queryKeys";
import { usersApi } from "../api/usersApi";
import { useUsersFilterStore } from "../store/usersFilterStore";

export const useUsers = () => {
  const { search, role, page, pageSize } = useUsersFilterStore();
  const filters = { search, role: role ?? undefined, page, pageSize };

  return useQuery({
    queryKey: queryKeys.users.list(filters),
    queryFn: () => usersApi.getUsers(filters),
    placeholderData: (previousData) => previousData,
  });
};
