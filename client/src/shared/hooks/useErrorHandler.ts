import { useCallback } from "react";
import { toast } from "sonner";
import { AppError } from "@/core/error/AppError";

export const useErrorHandler = () => {
  const handleError = useCallback((error: unknown) => {
    if (error instanceof AppError) {
      if (error.isForbidden) {
        toast.error("You do not have permission to perform this action");
        return;
      }
      if (error.isNotFound) {
        toast.error("The requested resource was not found");
        return;
      }
      if (error.isServerError) {
        toast.error("A server error occurred. Please try again later.");
        return;
      }
      toast.error(error.message);
      return;
    }

    if (error instanceof Error) {
      toast.error(error.message);
      return;
    }

    toast.error("An unexpected error occurred");
  }, []);

  return { handleError };
};