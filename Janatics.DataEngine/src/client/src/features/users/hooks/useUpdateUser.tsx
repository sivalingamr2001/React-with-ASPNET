import { useMutation, useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "@/core/query/queryKeys";
import { usersApi } from "../api/usersApi";
import type { UpdateUserPayload, User } from "../types/user.types";
import { toast } from "sonner";

export const useUpdateUser = (userId: string) => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: UpdateUserPayload) =>
      usersApi.updateUser(userId, payload),

    // Optimistic update — update cache before the request completes
    onMutate: async (payload) => {
      await queryClient.cancelQueries({
        queryKey: queryKeys.users.detail(userId),
      });

      const previousUser = queryClient.getQueryData<User>(
        queryKeys.users.detail(userId)
      );

      queryClient.setQueryData<User>(queryKeys.users.detail(userId), (old) =>
        old ? { ...old, ...payload } : old
      );

      return { previousUser };
    },

    onError: (error, _variables, context) => {
      // Rollback on failure
      if (context?.previousUser) {
        queryClient.setQueryData(
          queryKeys.users.detail(userId),
          context.previousUser
        );
      }
      toast.error(error.message ?? "Failed to update user");
    },

    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: queryKeys.users.detail(userId),
      });
      queryClient.invalidateQueries({ queryKey: queryKeys.users.lists() });
      toast.success("User updated successfully");
    },
  });
};