import type { UserRole } from "@/shared/types/common.types";

export interface User {
  id: string;
  email: string;
  name: string;
  role: UserRole;
  status: "active" | "inactive";
  department?: string;
  createdAt: string;
  updatedAt: string;
}

export type CreateUserPayload = Pick<User, "email" | "name" | "role"> & {
  password: string;
};

export type UpdateUserPayload = Partial<
  Pick<User, "name" | "role" | "status" | "department">
>;