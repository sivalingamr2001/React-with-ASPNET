import { HashRouter } from "react-router-dom";

import AdminShell from "./shell";

export default function AdminPortal() {
  return (
    <HashRouter>
      <AdminShell />
    </HashRouter>
  );
}
