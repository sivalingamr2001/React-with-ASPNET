import { useMutation, useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "@/core/query/queryKeys";
import { usersApi } from "../api/usersApi";
import type { CreateUserPayload } from "../types/user.types";
import { toast } from "sonner";

export const useCreateUser = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: CreateUserPayload) => usersApi.createUser(payload),

    onSuccess: (newUser) => {
      // Invalidate the user list cache — triggers refetch for all list queries
      queryClient.invalidateQueries({ queryKey: queryKeys.users.lists() });

      // Optimistically seed the detail cache
      queryClient.setQueryData(queryKeys.users.detail(newUser.id), newUser);

      toast.success(`User ${newUser.name} created successfully`);
    },

    onError: (error) => {
      toast.error(error.message ?? "Failed to create user");
    },
  });
};