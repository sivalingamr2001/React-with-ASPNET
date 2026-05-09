import { useMutation } from "@tanstack/react-query";
import { useNavigate, useLocation } from "react-router-dom";
import { useAuthStore } from "@/core/store/authStore";
import { authService } from "@/core/auth/authService";
import type { LoginFormValues } from "../schemas/loginSchema";
import { toast } from "sonner";

export const useLogin = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const setCredentials = useAuthStore((s) => s.setCredentials);

  const from =
    (location.state as { from?: Location })?.from?.pathname ?? "/dashboard";

  return useMutation({
    mutationFn: (values: LoginFormValues) =>
      authService.login({
        email: values.email,
        password: values.password,
      }),

    onSuccess: (data) => {
      setCredentials(data.user, data.accessToken);
      navigate(from, { replace: true });
    },

    onError: (error) => {
      toast.error(error.message ?? "Invalid credentials");
    },
  });
};