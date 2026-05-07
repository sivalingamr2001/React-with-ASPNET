'use client';

import { motion } from 'framer-motion';
import { TrendingUp, Users, Zap, Activity, ArrowUpRight } from 'lucide-react';

interface Card {
  id: string;
  title: string;
  description: string;
  icon: any;
  value?: string;
  trend?: string;
  colSpan?: number;
  rowSpan?: number;
  gradient?: string;
}

const cards: Card[] = [
  {
    id: '1',
    title: 'Revenue',
    description: 'Total monthly revenue',
    icon: TrendingUp,
    value: '$45,231.89',
    trend: '+20.1%',
    colSpan: 2,
    gradient: 'from-blue-600 to-cyan-600',
  },
  {
    id: '2',
    title: 'Active Users',
    description: 'Currently active users',
    icon: Users,
    value: '2,543',
    trend: '+12.5%',
    gradient: 'from-purple-600 to-pink-600',
  },
  {
    id: '3',
    title: 'Performance',
    description: 'System uptime',
    icon: Zap,
    value: '99.8%',
    trend: '+2.3%',
    gradient: 'from-orange-600 to-red-600',
  },
  {
    id: '4',
    title: 'Activity',
    description: 'Events this month',
    icon: Activity,
    value: '12,456',
    trend: '-4.2%',
    colSpan: 2,
    gradient: 'from-green-600 to-emerald-600',
  },
];

const containerVariants = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: {
      staggerChildren: 0.1,
      delayChildren: 0.2,
    },
  },
};

const itemVariants = {
  hidden: { opacity: 0, y: 20, scale: 0.95 },
  visible: {
    opacity: 1,
    y: 0,
    scale: 1,
    transition: {
      duration: 0.5,
      ease: [0.23, 1, 0.320, 1],
    },
  },
};

export function DashboardGrid() {
  return (
    <motion.div
      variants={containerVariants}
      initial="hidden"
      animate="visible"
      className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 auto-rows-fr"
    >
      {cards.map((card) => {
        const Icon = card.icon;
        const colSpanClass = card.colSpan === 2 ? 'md:col-span-2' : 'md:col-span-1';

        return (
          <motion.div
            key={card.id}
            variants={itemVariants}
            className={`${colSpanClass} group`}
          >
            <motion.div
              className="relative h-full p-6 rounded-xl border border-zinc-800 bg-zinc-900/40 backdrop-blur-sm overflow-hidden cursor-pointer"
              whileHover={{
                borderColor: 'rgb(39, 39, 42)',
                backgroundColor: 'rgba(24, 24, 27, 0.6)',
              }}
              transition={{ duration: 0.3 }}
            >
              {/* Gradient background on hover */}
              <motion.div
                className={`absolute inset-0 bg-gradient-to-br ${card.gradient} opacity-0 transition-opacity duration-300`}
                whileHover={{ opacity: 0.05 }}
                aria-hidden="true"
              />

              {/* Glow effect */}
              <motion.div
                className="absolute -inset-12 bg-gradient-to-br from-blue-500/0 to-purple-500/0 opacity-0 blur-2xl group-hover:opacity-50 transition-opacity duration-300 pointer-events-none"
                aria-hidden="true"
              />

              {/* Content */}
              <div className="relative z-10">
                <div className="flex items-center justify-between mb-4">
                  <motion.div
                    className={`p-2.5 rounded-lg bg-gradient-to-br ${card.gradient} text-white`}
                    whileHover={{ scale: 1.1, rotate: 5 }}
                    transition={{ type: 'spring', damping: 15, stiffness: 200 }}
                  >
                    <Icon className="w-5 h-5" />
                  </motion.div>
                  {card.trend && (
                    <motion.div
                      className={`text-sm font-semibold flex items-center gap-1 ${
                        card.trend.startsWith('+')
                          ? 'text-green-400'
                          : 'text-red-400'
                      }`}
                      initial={{ opacity: 0, x: -10 }}
                      whileInView={{ opacity: 1, x: 0 }}
                      transition={{ delay: 0.3 }}
                    >
                      {card.trend}
                      <ArrowUpRight
                        className={`w-4 h-4 transition-transform ${
                          card.trend.startsWith('-') ? 'rotate-180' : ''
                        }`}
                      />
                    </motion.div>
                  )}
                </div>

                <h3 className="text-sm font-medium text-zinc-400 mb-1">
                  {card.title}
                </h3>
                <p className="text-xs text-zinc-600 mb-4">{card.description}</p>

                {card.value && (
                  <motion.div
                    className="text-2xl font-bold text-foreground"
                    initial={{ opacity: 0 }}
                    whileInView={{ opacity: 1 }}
                    transition={{ delay: 0.2 }}
                  >
                    {card.value}
                  </motion.div>
                )}
              </div>

              {/* Bottom border accent on hover */}
              <motion.div
                className="absolute bottom-0 left-0 right-0 h-px bg-gradient-to-r from-transparent via-blue-500 to-transparent opacity-0"
                whileHover={{ opacity: 1 }}
                transition={{ duration: 0.3 }}
              />
            </motion.div>
          </motion.div>
        );
      })}
    </motion.div>
  );
}
