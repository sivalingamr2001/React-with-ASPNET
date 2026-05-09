import { z } from "zod";

const envSchema = z.object({
  VITE_API_BASE_URL: z.string().url("VITE_API_BASE_URL must be a valid URL"),
  VITE_APP_ENV: z.enum(["development", "staging", "production"]),
  VITE_ENABLE_DEVTOOLS: z
    .string()
    .transform((v) => v === "true")
    .pipe(z.boolean()),
  VITE_APP_VERSION: z.string().optional().default("0.0.0"),
});

type Env = z.infer<typeof envSchema>;

const parseEnv = (): Env => {
  const result = envSchema.safeParse(import.meta.env);

  if (!result.success) {
    const formatted = result.error.issues
      .map((issue) => `  • ${issue.path.join(".")}: ${issue.message}`)
      .join("\n");

    throw new Error(
      `Environment variable validation failed:\n${formatted}\n\nCheck your .env files.`
    );
  }

  return result.data;
};

// Throw at startup — fail loudly before rendering anything
export const env = parseEnv();