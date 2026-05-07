'use client';

import { motion } from 'framer-motion';

const skeletonVariants = {
  loading: {
    opacity: [0.3, 0.5, 0.3],
    transition: {
      duration: 2,
      repeat: Infinity,
      ease: 'easeInOut',
    },
  },
};

export function DashboardSkeleton() {
  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 auto-rows-fr">
      {[...Array(4)].map((_, i) => (
        <motion.div
          key={i}
          className={`${i === 0 || i === 3 ? 'md:col-span-2' : 'md:col-span-1'}`}
          variants={skeletonVariants}
          animate="loading"
        >
          <div className="h-full p-6 rounded-xl border border-zinc-800 bg-zinc-900/40 overflow-hidden">
            {/* Icon skeleton */}
            <div className="w-10 h-10 bg-zinc-800 rounded-lg mb-4" />

            {/* Title skeleton */}
            <div className="h-4 bg-zinc-800 rounded w-24 mb-2" />

            {/* Description skeleton */}
            <div className="h-3 bg-zinc-800 rounded w-32 mb-4" />

            {/* Value skeleton */}
            <div className="h-8 bg-zinc-800 rounded w-40" />
          </div>
        </motion.div>
      ))}
    </div>
  );
}
