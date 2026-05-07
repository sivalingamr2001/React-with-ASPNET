import axios, { type AxiosInstance } from "axios";
import { env } from "@/config/env";

export const createAxiosInstance = (): AxiosInstance => {
  const instance = axios.create({
    baseURL: env.VITE_API_BASE_URL,
    timeout: 30_000,
    headers: {
      "Content-Type": "application/json",
      Accept: "application/json",
    },
  });

  return instance;
};

export const axiosInstance = createAxiosInstance();