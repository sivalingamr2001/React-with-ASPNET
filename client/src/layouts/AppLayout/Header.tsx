import gsap from 'gsap';
import {
  Activity,
  Bell,
  ChevronDown,
  Coffee,
  Command,
  LayoutGrid,
  Menu,
  MessageSquare,
  Moon,
  Plus,
  Search,
  Settings,
  Sparkles,
  Star,
  Sun,
  TrendingUp,
  User,
  X,
  Zap
} from 'lucide-react';
import { useCallback, useEffect, useRef, useState } from 'react';

// ─── Types ─────────────────────────────────────────────────────────────

interface Action {
  id: string;
  icon: React.ReactNode;
  label: string;
  color?: string;
  shortcut?: string;
  badge?: number;
}

interface UserProfile {
  name: string;
  role: string;
  avatar?: string;
  status?: 'online' | 'away' | 'focus' | 'dnd';
}

interface Notification {
  id: string;
  title: string;
  message: string;
  time: string;
  type: 'info' | 'success' | 'warning' | 'urgent';
  read: boolean;
}

interface SearchResult {
  id: string;
  title: string;
  subtitle: string;
  icon: React.ReactNode;
  category: string;
  shortcut?: string;
}

type TimeOfDay = 'morning' | 'focus' | 'evening' | 'night';

// ─── Theme Config ──────────────────────────────────────────────────────

const THEME_CONFIG: Record<TimeOfDay, {
  gradient: string;
  accent: string;
  glow: string;
  description: string;
}> = {
  morning: {
    gradient: 'from-amber-500/20 via-orange-500/10 to-transparent',
    accent: 'text-amber-500',
    glow: 'shadow-amber-500/30',
    description: 'Energized for the day'
  },
  focus: {
    gradient: 'from-emerald-500/20 via-teal-500/10 to-transparent',
    accent: 'text-emerald-500',
    glow: 'shadow-emerald-500/30',
    description: 'Deep work mode'
  },
  evening: {
    gradient: 'from-violet-500/20 via-fuchsia-500/10 to-transparent',
    accent: 'text-violet-500',
    glow: 'shadow-violet-500/30',
    description: 'Winding down'
  },
  night: {
    gradient: 'from-cyan-500/20 via-blue-500/10 to-transparent',
    accent: 'text-cyan-500',
    glow: 'shadow-cyan-500/30',
    description: 'Late night hustle'
  }
};

// ─── Mock Data ─────────────────────────────────────────────────────────

const MOCK_NOTIFICATIONS: Notification[] = [
  { id: '1', title: 'Deployment Successful', message: 'Production build v2.4.0 is live', time: '2m ago', type: 'success', read: false },
  { id: '2', title: 'New Team Member', message: 'Sarah Chen joined the design team', time: '1h ago', type: 'info', read: false },
  { id: '3', title: 'Performance Alert', message: 'API latency spike detected', time: '3h ago', type: 'warning', read: true },
  { id: '4', title: 'Security Update', message: 'Critical patch available', time: '5h ago', type: 'urgent', read: true },
];

const MOCK_SEARCH_RESULTS: SearchResult[] = [
  { id: '1', title: 'Dashboard Overview', subtitle: 'Jump to main dashboard', icon: <LayoutGrid size={16} />, category: 'Navigation', shortcut: '⌘1' },
  { id: '2', title: 'Team Settings', subtitle: 'Manage team members', icon: <User size={16} />, category: 'Settings', shortcut: '⌘,' },
  { id: '3', title: 'Analytics Report', subtitle: 'Q4 performance metrics', icon: <TrendingUp size={16} />, category: 'Reports', shortcut: '⌘R' },
  { id: '4', title: 'Messages', subtitle: '3 unread conversations', icon: <MessageSquare size={16} />, category: 'Communication', shortcut: '⌘M' },
  { id: '5', title: 'System Preferences', subtitle: 'Customize your workspace', icon: <Settings size={16} />, category: 'Settings' },
];

// ─── Component ─────────────────────────────────────────────────────────

export const Header = ({
  brandName = "JANATICS",
  actions = [
    {
      id: 'create',
      icon: <Plus size={18} />,
      label: 'New Project',
      color: 'bg-gradient-to-r from-primary to-primary/80',
      shortcut: '⌘N'
    },
    {
      id: 'ai',
      icon: <Sparkles size={18} />,
      label: 'AI Assistant',
      color: 'bg-gradient-to-r from-violet-600 to-fuchsia-600',
      shortcut: '⌘Shift+A'
    }
  ],
  user = { name: "Admin", role: "Superuser", status: 'online' as const }
}) => {
  // Refs
  const headerRef = useRef<HTMLElement>(null);
  const searchRef = useRef<HTMLDivElement>(null);
  const searchInputRef = useRef<HTMLInputElement>(null);
  const brandRef = useRef<HTMLDivElement>(null);
  const commandPaletteRef = useRef<HTMLDivElement>(null);
  const notificationsRef = useRef<HTMLDivElement>(null);

  // State
  const [isSearchOpen, setIsSearchOpen] = useState(false);
  const [isCommandPaletteOpen, setIsCommandPaletteOpen] = useState(false);
  const [isNotificationsOpen, setIsNotificationsOpen] = useState(false);
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedResult, setSelectedResult] = useState(0);
  const [timeOfDay, setTimeOfDay] = useState<TimeOfDay>('focus');
  const [notifications, setNotifications] = useState(MOCK_NOTIFICATIONS);
  const [hoveredAction, setHoveredAction] = useState<string | null>(null);
  const [scrolled, setScrolled] = useState(false);

  // Derived state
  const unreadCount = notifications.filter(n => !n.read).length;
  const theme = THEME_CONFIG[timeOfDay];
  const filteredResults = MOCK_SEARCH_RESULTS.filter(r =>
    r.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
    r.subtitle.toLowerCase().includes(searchQuery.toLowerCase())
  );

  // ─── Effects ─────────────────────────────────────────────────────────

  // Entrance animation with persistence
  useEffect(() => {
    const hasAnimated = sessionStorage.getItem('header-played-v2');

    if (!hasAnimated) {
      const tl = gsap.timeline();

      tl.fromTo(headerRef.current,
        { y: -100, opacity: 0 },
        { y: 0, opacity: 1, duration: 1.2, ease: "expo.out" }
      )
        .fromTo(brandRef.current,
          { opacity: 0, x: -20 },
          { opacity: 1, x: 0, duration: 0.8, ease: "power3.out" },
          "-=0.6"
        )
        .fromTo('.header-action',
          { opacity: 0, y: -10, scale: 0.9 },
          { opacity: 1, y: 0, scale: 1, duration: 0.5, stagger: 0.1, ease: "back.out(1.7)" },
          "-=0.4"
        );

      sessionStorage.setItem('header-played-v2', 'true');
    } else {
      gsap.set(headerRef.current, { y: 0, opacity: 1 });
    }
  }, []);

  // Detect time of day for emotional theming
  useEffect(() => {
    const hour = new Date().getHours();
    if (hour >= 5 && hour < 9) setTimeOfDay('morning');
    else if (hour >= 9 && hour < 17) setTimeOfDay('focus');
    else if (hour >= 17 && hour < 21) setTimeOfDay('evening');
    else setTimeOfDay('night');
  }, []);

  // Scroll detection for glass intensity
  useEffect(() => {
    const handleScroll = () => setScrolled(window.scrollY > 20);
    window.addEventListener('scroll', handleScroll, { passive: true });
    return () => window.removeEventListener('scroll', handleScroll);
  }, []);

  // Keyboard shortcuts
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Cmd+K for command palette
      if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
        e.preventDefault();
        setIsCommandPaletteOpen(prev => !prev);
      }
      // Escape to close
      if (e.key === 'Escape') {
        setIsCommandPaletteOpen(false);
        setIsNotificationsOpen(false);
        setIsSearchOpen(false);
      }
      // Arrow navigation in command palette
      if (isCommandPaletteOpen) {
        if (e.key === 'ArrowDown') {
          e.preventDefault();
          setSelectedResult(prev => (prev + 1) % filteredResults.length);
        }
        if (e.key === 'ArrowUp') {
          e.preventDefault();
          setSelectedResult(prev => (prev - 1 + filteredResults.length) % filteredResults.length);
        }
        if (e.key === 'Enter' && filteredResults[selectedResult]) {
          console.log('Selected:', filteredResults[selectedResult].title);
          setIsCommandPaletteOpen(false);
        }
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isCommandPaletteOpen, filteredResults, selectedResult]);

  // Focus search input when command palette opens
  useEffect(() => {
    if (isCommandPaletteOpen) {
      setTimeout(() => searchInputRef.current?.focus(), 100);
    }
  }, [isCommandPaletteOpen]);

  // ─── Handlers ────────────────────────────────────────────────────────

  const handleSearchToggle = useCallback(() => {
    const newState = !isSearchOpen;
    setIsSearchOpen(newState);
    gsap.to(searchRef.current, {
      width: newState ? 320 : 200,
      duration: 0.6,
      ease: "elastic.out(1, 0.8)"
    });
  }, [isSearchOpen]);

  const markAllRead = () => {
    setNotifications(prev => prev.map(n => ({ ...n, read: true })));
    gsap.to('.notification-badge', { scale: 0, duration: 0.3, ease: "back.in(2)" });
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'online': return 'bg-emerald-500';
      case 'away': return 'bg-amber-500';
      case 'focus': return 'bg-violet-500';
      case 'dnd': return 'bg-rose-500';
      default: return 'bg-slate-500';
    }
  };

  const getNotificationColor = (type: string) => {
    switch (type) {
      case 'success': return 'bg-emerald-500/20 text-emerald-400 border-emerald-500/30';
      case 'warning': return 'bg-amber-500/20 text-amber-400 border-amber-500/30';
      case 'urgent': return 'bg-rose-500/20 text-rose-400 border-rose-500/30';
      default: return 'bg-blue-500/20 text-blue-400 border-blue-500/30';
    }
  };

  // ─── Render ──────────────────────────────────────────────────────────

  return (
    <>
      <header
        ref={headerRef}
        className={`sticky top-0 z-50 w-full transition-all duration-500 ${scrolled
          ? 'bg-background/40 backdrop-blur-2xl border-b border-border/40 shadow-2xl shadow-black/5'
          : 'bg-background/20 backdrop-blur-xl border-b border-border/20'
          }`}
      >
        {/* Dynamic ambient glow */}
        <div className={`absolute inset-0 bg-gradient-to-r ${theme.gradient} opacity-50 pointer-events-none transition-all duration-1000`} />

        {/* Noise texture overlay */}
        <div className="absolute inset-0 opacity-[0.03] pointer-events-none mix-blend-overlay"
          style={{ backgroundImage: `url("data:image/svg+xml,%3Csvg viewBox='0 0 256 256' xmlns='http://www.w3.org/2000/svg'%3E%3Cfilter id='n'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='0.9' numOctaves='4' stitchTiles='stitch'/%3E%3C/filter%3E%3Crect width='100%25' height='100%25' filter='url(%23n)'/%3E%3C/svg%3E")` }}
        />

        <div className="relative mx-auto flex h-18 max-w-7xl items-center justify-between px-6">

          {/* ═════════════════════════════════════════════════════════
              LEFT: Branding & Navigation
          ═════════════════════════════════════════════════════════ */}
          <div className="flex items-center gap-8">
            {/* Brand Logo with Kinetic Typography */}
            <div ref={brandRef} className="flex items-center gap-3 group cursor-pointer">
              <div className={`relative flex h-10 w-10 items-center justify-center rounded-2xl bg-gradient-to-br from-primary via-primary to-primary/70 text-primary-foreground shadow-lg ${theme.glow} transition-all duration-500 group-hover:scale-110 group-hover:rotate-3`}>
                <span className="text-xl font-black italic tracking-tighter">{brandName[0]}</span>
                {/* Shine effect */}
                <div className="absolute inset-0 rounded-2xl bg-gradient-to-tr from-white/40 to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-300" />
              </div>

              <div className="flex flex-col">
                <span className="text-sm font-bold tracking-tight text-foreground group-hover:text-primary transition-colors">
                  {brandName}
                </span>
                <div className="flex items-center gap-1.5">
                  <span className="text-[10px] font-medium text-muted-foreground uppercase tracking-widest">Portal</span>
                  <span className={`inline-block w-1.5 h-1.5 rounded-full ${getStatusColor(user.status || 'online')} animate-pulse`} />
                </div>
              </div>

              <ChevronDown size={14} className="text-muted-foreground transition-transform duration-300 group-hover:translate-y-0.5 group-hover:rotate-180" />
            </div>

            {/* Desktop Navigation with Bento-style pills */}
            <nav className="hidden xl:flex items-center gap-1">
              {[
                { label: 'Overview', icon: <Activity size={14} />, active: true },
                { label: 'Team', icon: <User size={14} /> },
                { label: 'Reports', icon: <TrendingUp size={14} /> },
                { label: 'Starred', icon: <Star size={14} /> }
              ].map((item) => (
                <button
                  key={item.label}
                  className={`relative flex items-center gap-2 rounded-xl px-4 py-2 text-sm font-semibold transition-all duration-300 ${item.active
                    ? 'bg-primary/10 text-primary shadow-inner shadow-primary/10'
                    : 'text-muted-foreground hover:bg-secondary/60 hover:text-foreground'
                    }`}
                >
                  {item.icon}
                  {item.label}
                  {item.active && (
                    <span className="absolute bottom-0 left-1/2 -translate-x-1/2 w-1 h-1 rounded-full bg-primary" />
                  )}
                </button>
              ))}
            </nav>
          </div>

          {/* ═════════════════════════════════════════════════════════
              CENTER: Intelligent Command Palette Trigger
          ═════════════════════════════════════════════════════════ */}
          <div className="hidden lg:flex items-center gap-4">
            <div
              ref={searchRef}
              onClick={() => setIsCommandPaletteOpen(true)}
              className="relative flex items-center gap-3 bg-secondary/30 border border-border/50 px-4 py-2.5 rounded-2xl cursor-text hover:bg-secondary/60 hover:border-primary/30 hover:shadow-lg hover:shadow-primary/5 transition-all duration-300 w-[200px] group"
            >
              <Search size={16} className="text-muted-foreground group-hover:text-primary transition-colors" />
              <span className="text-xs font-medium text-muted-foreground truncate flex-1">
                Quick Search
              </span>
              <kbd className="rounded-lg bg-background/80 border border-border/50 px-2 py-0.5 text-[10px] font-mono text-muted-foreground shadow-sm">
                ⌘K
              </kbd>

              {/* Hover glow */}
              <div className="absolute inset-0 rounded-2xl bg-gradient-to-r from-primary/5 to-transparent opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none" />
            </div>

            {/* Time-of-day indicator */}
            <div className="hidden md:flex items-center gap-2 px-3 py-1.5 rounded-full bg-secondary/30 border border-border/30">
              {timeOfDay === 'morning' && <Sun size={14} className="text-amber-500" />}
              {timeOfDay === 'focus' && <Zap size={14} className="text-emerald-500" />}
              {timeOfDay === 'evening' && <Coffee size={14} className="text-violet-500" />}
              {timeOfDay === 'night' && <Moon size={14} className="text-cyan-500" />}
              <span className="text-[10px] font-medium text-muted-foreground capitalize">{timeOfDay}</span>
            </div>
          </div>

          {/* ═════════════════════════════════════════════════════════
              RIGHT: Actions, Intelligence Hub & Profile
          ═════════════════════════════════════════════════════════ */}
          <div className="flex items-center gap-3">

            {/* Action Slots with Tactile Effects */}
            <div className="hidden sm:flex items-center gap-2 border-r border-border/40 pr-4">
              {actions.map((action, index) => (
                <button
                  key={action.id}
                  className={`header-action relative flex items-center gap-2 px-4 py-2 rounded-xl text-sm font-bold text-white transition-all duration-300 hover:brightness-110 hover:scale-105 active:scale-95 ${action.color || 'bg-secondary text-foreground'}`}
                  onMouseEnter={() => setHoveredAction(action.id)}
                  onMouseLeave={() => setHoveredAction(null)}
                >
                  <span className="relative z-10">{action.icon}</span>
                  <span className="hidden xl:inline relative z-10">{action.label}</span>

                  {/* Magnetic hover effect */}
                  <div className={`absolute inset-0 rounded-xl bg-white/20 opacity-0 transition-opacity duration-300 ${hoveredAction === action.id ? 'opacity-100' : ''}`} />

                  {/* Shortcut tooltip */}
                  {action.shortcut && hoveredAction === action.id && (
                    <span className="absolute -bottom-8 left-1/2 -translate-x-1/2 px-2 py-1 rounded-md bg-popover border border-border text-[10px] font-mono text-muted-foreground whitespace-nowrap shadow-lg z-50">
                      {action.shortcut}
                    </span>
                  )}
                </button>
              ))}
            </div>

            {/* Intelligence Hub: Notifications */}
            <div className="relative">
              <button
                className="relative rounded-xl p-2.5 text-muted-foreground hover:bg-secondary/60 hover:text-foreground transition-all duration-300 hover:scale-110 active:scale-95"
                onClick={() => setIsNotificationsOpen(!isNotificationsOpen)}
              >
                <Bell size={20} />
                {unreadCount > 0 && (
                  <span className="notification-badge absolute right-1.5 top-1.5 flex h-4 w-4 items-center justify-center rounded-full bg-destructive text-[9px] font-bold text-white animate-pulse shadow-lg shadow-destructive/30">
                    {unreadCount}
                  </span>
                )}
              </button>

              {/* Notifications Dropdown */}
              {isNotificationsOpen && (
                <div
                  ref={notificationsRef}
                  className="absolute right-0 top-full mt-3 w-80 rounded-2xl bg-popover/95 backdrop-blur-xl border border-border/50 shadow-2xl shadow-black/10 p-4 z-50"
                >
                  <div className="flex items-center justify-between mb-3">
                    <h3 className="text-sm font-bold">Notifications</h3>
                    <button
                      onClick={markAllRead}
                      className="text-[10px] text-primary hover:underline font-medium"
                    >
                      Mark all read
                    </button>
                  </div>
                  <div className="space-y-2 max-h-64 overflow-y-auto">
                    {notifications.map((notif) => (
                      <div
                        key={notif.id}
                        className={`flex gap-3 p-2.5 rounded-xl border transition-all duration-200 ${getNotificationColor(notif.type)} ${!notif.read ? 'bg-opacity-30' : 'opacity-60'}`}
                      >
                        <div className="flex-1 min-w-0">
                          <p className="text-xs font-bold truncate">{notif.title}</p>
                          <p className="text-[10px] opacity-80 truncate">{notif.message}</p>
                          <p className="text-[9px] opacity-60 mt-1">{notif.time}</p>
                        </div>
                        {!notif.read && <div className="w-1.5 h-1.5 rounded-full bg-current mt-1.5 shrink-0" />}
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>

            {/* User Profile Hub with Spatial Depth */}
            <div className="flex items-center gap-3 group cursor-pointer pl-2">
              <div className="text-right hidden md:block">
                <p className="text-xs font-bold text-foreground leading-tight">{user.name}</p>
                <p className="text-[10px] font-medium text-muted-foreground uppercase tracking-wider">{user.role}</p>
              </div>

              <div className="relative h-10 w-10">
                {/* Animated aura */}
                <div className={`absolute -inset-1 rounded-full bg-gradient-to-tr from-primary to-cyan-400 opacity-20 group-hover:opacity-60 transition-all duration-500 blur-md group-hover:blur-lg`} />

                {/* Status ring */}
                <div className={`absolute -inset-0.5 rounded-full border-2 ${getStatusColor(user.status || 'online')} opacity-50 group-hover:opacity-100 transition-opacity`} />

                <div className="relative h-full w-full rounded-full border-2 border-background bg-secondary flex items-center justify-center overflow-hidden transition-transform duration-300 group-hover:scale-105">
                  <User size={20} className="text-foreground/70" />
                </div>
              </div>
            </div>

            {/* Mobile Menu Toggle */}
            <button
              className="lg:hidden p-2 text-foreground hover:bg-secondary/60 rounded-xl transition-colors"
              onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
            >
              {isMobileMenuOpen ? <X size={24} /> : <Menu size={24} />}
            </button>
          </div>
        </div>

        {/* Mobile Menu */}
        {isMobileMenuOpen && (
          <div className="lg:hidden border-t border-border/40 bg-background/95 backdrop-blur-xl p-4 space-y-2">
            {['Overview', 'Team', 'Reports', 'Starred'].map((item) => (
              <button key={item} className="w-full flex items-center gap-3 rounded-xl px-4 py-3 text-sm font-semibold text-muted-foreground hover:bg-secondary/50 hover:text-foreground transition-all text-left">
                {item}
              </button>
            ))}
          </div>
        )}
      </header>

      {/* ═══════════════════════════════════════════════════════════════
          COMMAND PALETTE OVERLAY
      ═══════════════════════════════════════════════════════════════ */}
      {isCommandPaletteOpen && (
        <div className="fixed inset-0 z-[100] flex items-start justify-center pt-[20vh] bg-black/40 backdrop-blur-sm" onClick={() => setIsCommandPaletteOpen(false)}>
          <div
            ref={commandPaletteRef}
            className="w-full max-w-xl bg-popover/95 backdrop-blur-2xl rounded-2xl border border-border/50 shadow-2xl shadow-black/20 overflow-hidden"
            onClick={e => e.stopPropagation()}
          >
            {/* Search Input */}
            <div className="flex items-center gap-3 px-6 py-4 border-b border-border/50">
              <Command size={20} className="text-muted-foreground" />
              <input
                ref={searchInputRef}
                type="text"
                placeholder="Search users, files, commands..."
                className="flex-1 bg-transparent text-sm font-medium placeholder:text-muted-foreground outline-none"
                value={searchQuery}
                onChange={(e) => {
                  setSearchQuery(e.target.value);
                  setSelectedResult(0);
                }}
              />
              <kbd className="rounded-lg bg-secondary px-2 py-1 text-[10px] font-mono text-muted-foreground">ESC</kbd>
            </div>

            {/* Results */}
            <div className="max-h-[400px] overflow-y-auto py-2">
              {filteredResults.length === 0 ? (
                <div className="px-6 py-8 text-center text-sm text-muted-foreground">
                  No results found for "<span className="text-foreground">{searchQuery}</span>"
                </div>
              ) : (
                <>
                  <div className="px-6 py-2 text-[10px] font-bold text-muted-foreground uppercase tracking-wider">
                    Suggestions
                  </div>
                  {filteredResults.map((result, index) => (
                    <button
                      key={result.id}
                      className={`w-full flex items-center gap-3 px-6 py-3 text-left transition-all duration-150 ${index === selectedResult
                        ? 'bg-primary/10 border-l-2 border-primary'
                        : 'hover:bg-secondary/50 border-l-2 border-transparent'
                        }`}
                      onMouseEnter={() => setSelectedResult(index)}
                      onClick={() => {
                        console.log('Selected:', result.title);
                        setIsCommandPaletteOpen(false);
                      }}
                    >
                      <div className={`p-2 rounded-lg ${index === selectedResult ? 'bg-primary/20 text-primary' : 'bg-secondary text-muted-foreground'}`}>
                        {result.icon}
                      </div>
                      <div className="flex-1">
                        <p className={`text-sm font-semibold ${index === selectedResult ? 'text-primary' : 'text-foreground'}`}>
                          {result.title}
                        </p>
                        <p className="text-xs text-muted-foreground">{result.subtitle}</p>
                      </div>
                      <div className="flex items-center gap-2">
                        <span className="text-[10px] text-muted-foreground bg-secondary px-2 py-0.5 rounded">{result.category}</span>
                        {result.shortcut && (
                          <kbd className="text-[10px] font-mono text-muted-foreground">{result.shortcut}</kbd>
                        )}
                      </div>
                    </button>
                  ))}
                </>
              )}
            </div>

            {/* Footer */}
            <div className="flex items-center justify-between px-6 py-3 border-t border-border/50 bg-secondary/20">
              <div className="flex items-center gap-4 text-[10px] text-muted-foreground">
                <span className="flex items-center gap-1"><span className="font-mono bg-secondary px-1 rounded">↑↓</span> Navigate</span>
                <span className="flex items-center gap-1"><span className="font-mono bg-secondary px-1 rounded">↵</span> Select</span>
              </div>
              <div className="flex items-center gap-2">
                <Sparkles size={12} className="text-primary" />
                <span className="text-[10px] font-medium text-primary">AI-Powered</span>
              </div>
            </div>
          </div>
        </div>
      )}
    </>
  );
};