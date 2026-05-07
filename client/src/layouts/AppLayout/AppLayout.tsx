import { useEffect, useRef } from "react";
import { Outlet, useLocation } from "react-router-dom";
import gsap from "gsap";
import { Header } from "./Header";
import { Sidebar } from "./Sidebar";

export const AppLayout = () => {
  const mainRef = useRef<HTMLDivElement>(null);
  const location = useLocation();

  useEffect(() => {
    // 1. Initial Page Load Animation
    const ctx = gsap.context(() => {
      const tl = gsap.timeline({ defaults: { ease: "expo.out" } });

      tl.from(".sidebar-container", { x: -100, opacity: 0, duration: 1.2 })
        .from(".header-container", { y: -50, opacity: 0, duration: 1 }, "-=0.8")
        .from(".main-content", { y: 20, opacity: 0, duration: 0.8 }, "-=0.6");
    });

    return () => ctx.revert();
  }, []);

  useEffect(() => {
    // 2. Route Change Transition
    // Smoothly fades in the content whenever the route changes
    gsap.fromTo(
      ".route-wrapper",
      { opacity: 0, y: 10 },
      { opacity: 1, y: 0, duration: 0.4, ease: "power2.out" }
    );
  }, [location.pathname]);

  return (
    <div className="relative min-h-screen overflow-hidden bg-background text-foreground selection:bg-primary/20">
      {/* Background Decorative Element - Architect's Touch */}
      <div className="fixed inset-0 -z-10 bg-[radial-gradient(ellipse_at_top_right,var(--tw-gradient-stops))] from-primary/5 via-transparent to-transparent" />

      <div className="header-container sticky top-0 z-50">
        <Header />
      </div>

      <div className="flex min-h-[calc(100vh-64px)] flex-col lg:flex-row">
        <aside className="sidebar-container w-full lg:w-72">
          <Sidebar />
        </aside>

        <main 
          ref={mainRef} 
          className="main-content flex-1 p-6 lg:p-10"
        >

          <div className="route-wrapper mx-auto max-w-7xl">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
};
