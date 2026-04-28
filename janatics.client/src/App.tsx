import React, { useEffect, useState } from "react";
import AdminPortal from "./apps/admin/Index";
import UserPortal from "./apps/user";
import { useAuth } from "./context/auth-provider";

function App() {
  const { user } = useAuth();
  const [isAdmin, setUserRole] = useState(false);

  useEffect(() => {
    if (user) {
      user.role === "Admin" ? setUserRole(true) : setUserRole(false);
    }
  }, [user]);

  return (
    <React.Fragment>
      {isAdmin ? <AdminPortal /> : <UserPortal />}
    </React.Fragment>
  );
}

export default App;
