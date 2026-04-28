export type UserType = {
  id: string;
  name: string;
  email: string;
  token: string;
  role?: "Admin" | "User" | string;
};
