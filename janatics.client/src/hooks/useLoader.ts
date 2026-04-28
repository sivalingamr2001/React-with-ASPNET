import React from "react";

export type LoaderKey = string | number | symbol;

export interface UseLoaderOptions {
  /**
   * Minimum time (ms) the loader stays visible so it never flashes
   * @default 300
   */
  minDuration?: number;
  /**
   * When tru, a rejected promise is re-thrown after the minimum
   * duration has elapsed. Set to false to swallow the errors silently.
   * @default true
   */
  rethrowErrors?: boolean;
}

export interface UseLoaderReturn {
  /** True while any tracked promise (or the minimum timer) is pending. */
  loading: boolean;

  /**
   * Wraps an async factory in the loader lifecycle
   *
   * @example
   * const data = await withLoader(() => fetchData());
   * const data = await withLoader(( => fetchUsers(), "users-list"));
   * */
  withLoader: <T>(fn: () => Promise<T>, key?: LoaderKey) => Promise<T>;

  /**
   * Imperatively start the loader. Useful for non-promise flows
   * Returns a `stop` function that ends this particular session.
   *
   * @example
   * const stop = start("upload");
   * // ... do work ...
   * stop();
   */
  start: (key?: LoaderKey) => () => void;

  /** How many independent loader sessions are active right now */
  activeCount: number;
}

//-----------------------------------------------------------------------------------------------------
// Hook
//-----------------------------------------------------------------------------------------------------

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
  const { minDuration = 3000, rethrowErrors = true } = options;

  //Ref tracks the true count synchronously (avoids stale-closure bugs).
  // State drives re-renders
  const countRef = React.useRef(0);
  const [activeCount, setActiveCount] = React.useState(0);

  // Sync helper - increment or decrement the ref AND flush to state.
  const mutate = React.useCallback((delta: 1 | -1) => {
    countRef.current += delta;
    setActiveCount(countRef.current);
  }, []);

  // ---- Imperative API -----------------------------------------------------------------------------------
  const start = React.useCallback(
    (_key?: LoaderKey): (() => void) => {
      mutate(1);
      let stopped = false;

      const stop = () => {
        if (stopped) return;
        stopped = true;
        mutate(-1);
      };

      return stop;
    },
    [mutate],
  );

  //---- Promise API --------------------------------------------------------------------------------------
  const withLoader = React.useCallback(
    async <T>(fn: () => Promise<T>, _key?: LoaderKey): Promise<T> => {
      mutate(1);

      // Start the minimum-duration timer immediately so it runs in
      // parallel with the async work - NOT sequentially after it.
      const minTimer = new Promise<void>((resolve) =>
        setTimeout(resolve, minDuration),
      );

      let result: T;
      let caughtError: unknown;
      let hasError = false;

      try {
        result = await fn();
      } catch (err) {
        hasError = true;
        caughtError = err;
      }

      //Always honour the minimum duration, even on rejection,
      // so the spinner never vanishes in under `minduration` ms.
      await minTimer;

      mutate(-1);

      if (hasError) {
        if (rethrowErrors) throw caughtError;
        return undefined as unknown as T;
      }

      return result!;
    },
    [mutate, minDuration, rethrowErrors],
  );

  return {
    loading: activeCount > 0,
    withLoader,
    start,
    activeCount,
  };
}
