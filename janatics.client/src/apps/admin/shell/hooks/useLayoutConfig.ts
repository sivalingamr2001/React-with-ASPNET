import { useEffect, useState } from "react";

import type { LayoutConfig } from "../types";

interface State {
  config: LayoutConfig | null;
  isLoading: boolean;
}

export function useLayoutConfig() {
  const [state, setState] = useState<State>({ config: null, isLoading: true });

  useEffect(() => {
    const load = async () => {
      try {
        const url = new URL("layout-config.json", window.location.origin + import.meta.env.BASE_URL + "/");
        const response = await fetch(url);
        if (!response.ok) throw new Error(`Failed to load layout config: ${response.status}`);
        const config = (await response.json()) as LayoutConfig;
        setState({ config, isLoading: false });
      } catch (error) {
        console.error("Unable to load admin layout config", error);
        setState({ config: null, isLoading: false });
      }
    };
    load();
  }, []);

  return state;
}
