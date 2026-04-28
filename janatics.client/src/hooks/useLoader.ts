import { useCallback, useEffect, useRef, useState } from "react";

/** Summary about this hook */
export type LoaderResult<T> =
  | { ok: true; data: T; error: null }
  | { ok: false; data: null; error: unknown };

// ─────────────────────────────────────────────────────────────────────────────
//  Public types
// ─────────────────────────────────────────────────────────────────────────────

export type LoaderKey = string;

export interface UseLoaderOptions {
  /**
   * Minimum time (ms) the loader stays visible — eliminates flash-of-loader.
   * Runs in PARALLEL with the async work via Promise.all, not sequentially.
   * @default 3000
   */
  minDuration?: number;

  /**
   * true  → withLoader throws on rejection (classic async/await pattern).
   * false → withLoader returns LoaderResult<T> — never throws; inspect .ok.
   * @default true
   */
  rethrowError?: boolean;
}

export interface UseLoaderReturn {
  /** True while any tracked session (or its minimum timer) is still pending. */
  loading: boolean;

  /** How many independent sessions are active right now. */
  activeCount: number;

  /**
   * Wraps an async factory in the loader lifecycle.
   *
   * rethrowError = true  (default) → returns T; throws on error.
   * rethrowError = false            → returns LoaderResult<T>; never throws.
   */
  withLoader: <T>(
    fn: () => Promise<T>,
    key?: LoaderKey,
  ) => Promise<T | LoaderResult<T>>;

  /**
   * Imperative API for non-promise flows (file uploads, WebSockets, etc.).
   * Returns an idempotent stop() function.
   *
   * @example
   * const stop = start("ws-session");
   * socket.on("close", stop);
   */
  start: (key?: LoaderKey) => () => void;
}

// ─────────────────────────────────────────────────────────────────────────────
//  useLoader
// ─────────────────────────────────────────────────────────────────────────────

/**
 * `useLoader` - A React hook to manage loading orchestrator state for async operations.
 *
 * Features:
 * - Reference-counted: N concurrent calls -> single `loading` state until all are done.
 * -Guaranteed minimum display time to prevent flashing for quick operations.
 * - Error-safe: minimum display time is honored even if promises reject, with optional re-throwing.
 * - key-tagged sessions: debug which call is pending.
 * - imperative start/stop escape-hatch for non-promise flows.
 * - Zero external dependencies beyond React itself.
 *
 * @example
 * //basic
 * const { loading, withLoader } = useLoader();
 * const users = await withLoader(() => api.getUsers());
 *
 * // Custom min duration
 * const { loading, withLoader } = useLoader({ minDuration: 500 });
 *
 * // Concurrent calls - loader stays on until BOTH finish
 * const [a, b] = await Promise.all([
 *   withLoader(() => api.fetchA(), "a"),
 *   withLoader(() => api.fetchB(), "b")
 * ]);
 */

export function useLoader(options: UseLoaderOptions = {}): UseLoaderReturn {
  // ── Fix #3: stable options ref ───────────────────────────────────────────
  const optionsRef = useRef<UseLoaderOptions>(options);
  useEffect(() => {
    optionsRef.current = options;
    // No dep array — runs every render, but is a ref assignment (no re-render).
  });

  // ── Fix #1: mount guard ──────────────────────────────────────────────────
  const isMountedRef = useRef(true);
  useEffect(() => {
    isMountedRef.current = true;
    return () => {
      isMountedRef.current = false;
    };
  }, []);

  // ── Reference counter (sync) + state (drives re-renders) ─────────────────
  const countRef = useRef(0);
  const [activeCount, setActiveCount] = useState(0);

  const mutate = useCallback((delta: 1 | -1) => {
    countRef.current = Math.max(0, countRef.current + delta);

    if (isMountedRef.current) {
      setActiveCount(() => countRef.current);
    }
  }, []);

  // ── Imperative API ────────────────────────────────────────────────────────
  const start = useCallback(
    (_key?: LoaderKey): (() => void) => {
      mutate(1);
      let stopped = false;

      return () => {
        if (stopped) return;
        stopped = true;
        mutate(-1);
      };
    },
    [mutate],
  );

  // ── Promise API ───────────────────────────────────────────────────────────
  const withLoader = useCallback(
    async <T>(
      fn: () => Promise<T>,
      _key?: LoaderKey,
    ): Promise<T | LoaderResult<T>> => {
      const { minDuration = 3000, rethrowError = true } = optionsRef.current;

      mutate(1);
      const minTimer = new Promise<void>((resolve) =>
        setTimeout(resolve, minDuration),
      );

      try {
        const [result] = await Promise.all([fn(), minTimer]);

        if (!rethrowError) {
          return {
            ok: true,
            data: result,
            error: null,
          } satisfies LoaderResult<T>;
        }

        return result;
      } catch (error) {
        await minTimer;

        if (!rethrowError) {
          return { ok: false, data: null, error } satisfies LoaderResult<T>;
        }

        throw error;
      } finally {
        mutate(-1);
      }
    },
    [mutate],
  );

  return {
    loading: activeCount > 0,
    withLoader,
    start,
    activeCount,
  };
}
