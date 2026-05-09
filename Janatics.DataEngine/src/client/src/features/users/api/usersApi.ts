import { z } from "zod";
import { axiosInstance } from "@/core/api/axiosInstance";
import type {
  CreateUserPayload,
  UpdateUserPayload,
  User,
} from "../types/user.types";
import type { PaginatedResponse } from "@/shared/types/api.types";

const USERS_BASE = "/users";

const userSchema = z.object({
  id: z.string().uuid(),
  email: z.string().email(),
  name: z.string(),
  role: z.enum(["admin", "manager", "viewer"]),
  status: z.enum(["active", "inactive"]),
  createdAt: z.string().datetime(),
});

export const usersApi = {
  getUsers: async (params: {
    page: number;
    pageSize: number;
    search?: string;
    role?: string;
  }): Promise<PaginatedResponse<User>> => {
    const { data } = await axiosInstance.get<PaginatedResponse<User>>(
      USERS_BASE,
      { params }
    );
    return data;
  },

  getUserById: async (userId: string): Promise<User> => {
    const { data } = await axiosInstance.get<unknown>(`${USERS_BASE}/${userId}`);
    return userSchema.parse(data) as User;
  },

  createUser: async (payload: CreateUserPayload): Promise<User> => {
    const { data } = await axiosInstance.post<User>(USERS_BASE, payload);
    return data;
  },

  updateUser: async (
    userId: string,
    payload: UpdateUserPayload
  ): Promise<User> => {
    const { data } = await axiosInstance.patch<User>(
      `${USERS_BASE}/${userId}`,
      payload
    );
    return data;
  },

  deleteUser: async (userId: string): Promise<void> => {
    await axiosInstance.delete(`${USERS_BASE}/${userId}`);
  },
};