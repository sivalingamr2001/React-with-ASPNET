import { Navigate, Route, Routes } from "react-router-dom";

import DashboardPage from "../features/dashboard";
import FieldMapperPage from "../features/field-mapper";
import QueryBuilderPage from "../features/query-builder";
import RegistryManager from "../pages/RegistryManager";
import type { LayoutConfig } from "./types";
import AppLayout from "./components/AppLayout";

interface Props {
  config: LayoutConfig;
}

export default function AdminRoutes({ config }: Props) {
  return (
    <Routes>
      <Route element={<AppLayout config={config} />} path="/">
        <Route element={<Navigate replace to={config.app.homeRoute} />} index />
        <Route
          element={<DashboardPage page={config.pages[0]} />}
          path={config.pages[0].route}
        />
        <Route
          element={<FieldMapperPage page={config.pages[1]} />}
          path={config.pages[1].route}
        />
        <Route
          element={<QueryBuilderPage page={config.pages[2]} />}
          path={config.pages[2].route}
        />
        <Route
          element={
            <RegistryManager
              page={{
                id: "registry",
                route: "/admin/registry",
                title: "Registry Manager",
                description:
                  "Manage database profiles, entities, and columns for the DataEngine.",
                icon: "Database",
                breadcrumb: ["Admin", "Registry"],
              }}
            />
          }
          path="/admin/registry"
        />
      </Route>
    </Routes>
  );
}
