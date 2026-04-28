import React from "react";
import { useAuth } from "../context/auth-provider";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";
import { toast } from "sonner";

export default function LoginPage() {
  const { login } = useAuth();
  const [loading, setLoading] = React.useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      await login("admin", "password123");
      toast.success("Login successful! Redirecting...");
    } catch (err) {
      toast.error(
        "Invalid credentials. Please try again. Using your dummy login logic: username 'admin' + password 'password123'",
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="grid min-h-screen grid-cols-1 lg:grid-cols-2">
      {/* --- Left Side: Branding / Testimonial --- */}
      <div className="relative hidden flex-col bg-zinc-900 p-10 text-white lg:flex">
        <div className="relative z-20 flex items-center text-lg font-medium">
          <svg
            className="mr-2 h-6 w-6"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            viewBox="0 0 24 24"
          >
            <path d="M15 6v12a3 3 0 1 0 3-3H6a3 3 0 1 0 3 3V6a3 3 0 1 0-3 3h12a3 3 0 1 0-3-3" />
          </svg>
          Acme Corp
        </div>
        <div className="relative z-20 mt-auto">
          <blockquote className="space-y-2">
            <p className="text-lg">
              "This platform has completely transformed how we manage our
              internal admin portals."
            </p>
            <footer className="text-sm text-zinc-400">
              Sofia Davis, Lead Architect
            </footer>
          </blockquote>
        </div>
      </div>

      {/* --- Right Side: Login Form --- */}
      <div className="flex items-center justify-center p-8">
        <div className="mx-auto flex w-full flex-col justify-center space-y-6 sm:w-[350px]">
          <div className="flex flex-col space-y-2 text-center">
            <h1 className="text-2xl font-semibold tracking-tight">
              Welcome back
            </h1>
            <p className="text-sm text-muted-foreground">
              Enter your details below to login
            </p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="username">Username</Label>
              <Input id="username" placeholder="admin" required />
            </div>
            <div className="space-y-2">
              <Label htmlFor="password">Password</Label>
              <Input
                id="password"
                type="password"
                placeholder="password123"
                required
              />
            </div>
            <Button className="w-full" type="submit" disabled={loading}>
              {loading ? "Authenticating..." : "Sign In"}
            </Button>
          </form>

          <p className="px-8 text-center text-sm text-muted-foreground">
            By clicking continue, you agree to our Terms of Service.
          </p>
        </div>
      </div>
    </div>
  );
}
