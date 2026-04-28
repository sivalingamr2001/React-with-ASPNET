import { AnimatePresence, motion } from "framer-motion";
import { Outlet, useLocation } from "react-router-dom";

interface Props {
  onScrollChange: (value: boolean) => void;
}

export default function Content({ onScrollChange }: Props) {
  const location = useLocation();
  const handleScroll = (event: React.UIEvent<HTMLDivElement>) =>
    onScrollChange(event.currentTarget.scrollTop > 8);

  return (
    <main className="min-h-screen flex-1 p-3 pl-20 lg:pl-3">
      <div className="h-[calc(100vh-1.5rem)] overflow-hidden rounded-[32px] border border-white/60 bg-white/45 shadow-[0_30px_120px_-48px_rgba(15,23,42,0.55)] backdrop-blur-xl">
        <div
          className="h-full overflow-y-auto p-4 md:p-6"
          onScroll={handleScroll}
        >
          <AnimatePresence mode="wait">
            <motion.div
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -12 }}
              initial={{ opacity: 0, y: 16 }}
              key={location.pathname}
              transition={{ duration: 0.25 }}
            >
              <Outlet />
            </motion.div>
          </AnimatePresence>
        </div>
      </div>
    </main>
  );
}
