import gsap from 'gsap';
import { Bell, ChevronDown, Menu, Plus, Search, User } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';

export const Header = ({
  brandName = "JANATICS",
  // Slot-based actions for maximum customizability
  actions = [
    { id: 'create', icon: <Plus size={18} />, label: 'New Project', color: 'bg-primary' },
  ],
  user = { name: "Admin", role: "Superuser" }
}) => {
  const headerRef = useRef(null);
  const searchRef = useRef(null);
  const [isSearchOpen, setIsSearchOpen] = useState(false);

  useEffect(() => {
    // Persistence check: prevents disappearing header on route changes
    const hasAnimated = sessionStorage.getItem('header-played');

    if (!hasAnimated) {
      gsap.fromTo(headerRef.current,
        { y: -100, opacity: 0 },
        {
          y: 0,
          opacity: 1,
          duration: 1.2,
          ease: "expo.out",
          onComplete: () => sessionStorage.setItem('header-played', 'true')
        }
      );
    } else {
      gsap.set(headerRef.current, { y: 0, opacity: 1 });
    }
  }, []);

  const handleSearchToggle = () => {
    const newState = !isSearchOpen;
    setIsSearchOpen(newState);
    gsap.to(searchRef.current, {
      width: newState ? 320 : 160,
      duration: 0.5,
      ease: "elastic.out(1, 0.8)"
    });
  };

  return (
    <header
      ref={headerRef}
      className="sticky top-0 z-50 w-full border-b border-border/40 bg-background/60 backdrop-blur-xl transition-all"
    >
      <div className="mx-auto flex h-16 max-w-384 items-center justify-between px-6">

        {/* LEFT: Branding & Workspace */}
        <div className="flex items-center gap-8">
          <div className="flex items-center gap-3 group cursor-pointer">
            <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-primary text-primary-foreground shadow-lg shadow-primary/20 transition-transform group-hover:scale-105">
              <span className="text-xl font-black italic">{brandName[0]}</span>
            </div>
            <div className="flex flex-col">
              <span className="text-sm font-bold tracking-tight text-foreground">{brandName}</span>
              <span className="text-[10px] font-medium text-muted-foreground uppercase tracking-widest">Portal</span>
            </div>
            <ChevronDown size={14} className="text-muted-foreground transition-transform group-hover:translate-y-0.5" />
          </div>

          <nav className="hidden xl:flex items-center gap-1">
            {['Overview', 'Team', 'Reports'].map((item) => (
              <button key={item} className="rounded-lg px-3 py-1.5 text-sm font-semibold text-muted-foreground hover:bg-secondary/50 hover:text-foreground transition-all">
                {item}
              </button>
            ))}
          </nav>
        </div>

        {/* CENTER: Morphing Search */}
        <div
          ref={searchRef}
          onClick={handleSearchToggle}
          className="relative hidden lg:flex items-center gap-3 bg-secondary/30 border border-border/50 px-4 py-2 rounded-2xl cursor-text hover:bg-secondary/50 transition-all w-40"
        >
          <Search size={16} className="text-muted-foreground" />
          <span className="text-xs font-medium text-muted-foreground truncate">
            {isSearchOpen ? 'Search users, files, commands...' : 'Quick Search'}
          </span>
          {!isSearchOpen && (
            <kbd className="absolute right-3 rounded bg-background px-1.5 py-0.5 text-[10px] font-mono text-muted-foreground">⌘K</kbd>
          )}
        </div>

        {/* RIGHT: Dynamic Actions & Intelligence Hub */}
        <div className="flex items-center gap-4">

          {/* Action Slots Container */}
          <div className="hidden sm:flex items-center gap-2 border-r border-border/60 pr-4">
            {actions.map((action) => (
              <button
                key={action.id}
                className={`flex items-center gap-2 px-3 py-1.5 rounded-xl text-sm font-bold transition-all hover:brightness-110 active:scale-95 ${action.color || 'bg-secondary text-foreground'}`}
              >
                {action.icon}
                <span className="hidden xl:inline">{action.label}</span>
              </button>
            ))}

            <button className="relative rounded-xl p-2 text-muted-foreground hover:bg-secondary transition-all">
              <Bell size={20} />
              <span className="absolute right-2 top-2 h-2 w-2 rounded-full bg-destructive animate-pulse" />
            </button>
          </div>

          {/* User Profile Hub */}
          <div className="flex items-center gap-3 group cursor-pointer pl-2">
            <div className="text-right hidden md:block">
              <p className="text-xs font-bold text-foreground leading-tight">{user.name}</p>
              <p className="text-[10px] font-medium text-muted-foreground uppercase">{user.role}</p>
            </div>

            <div className="relative h-10 w-10">
              <div className="absolute inset-0 rounded-full bg-linear-to-tr from-primary to-cyan-400 opacity-20 group-hover:opacity-100 transition-opacity blur-md" />
              <div className="relative h-full w-full rounded-full border-2 border-background bg-secondary flex items-center justify-center overflow-hidden transition-transform group-hover:scale-105">
                <User size={20} className="text-foreground/70" />
              </div>
            </div>
          </div>

          <button className="lg:hidden p-2 text-foreground">
            <Menu size={24} />
          </button>
        </div>
      </div>
    </header>
  );
};
