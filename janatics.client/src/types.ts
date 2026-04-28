export type User = {
  id: string;
  name: string;
  email: string;
  token: string;
  role?: "Admin" | "User" | string;
};
