import { useEffect, useRef } from 'react';
import gsap from 'gsap';

export const PageLoader = () => {
  const containerRef = useRef(null);
  const spinnerRef = useRef(null);
  const textRef = useRef(null);

  useEffect(() => {
    const ctx = gsap.context(() => {
      gsap.fromTo(containerRef.current,
        { opacity: 0, y: 20, scale: 0.9 },
        { opacity: 1, y: 0, scale: 1, duration: 0.8, ease: "power4.out" }
      );

      // Infinite rotation
      gsap.to(spinnerRef.current, {
        rotation: 360,
        duration: 1.2,
        repeat: -1,
        ease: "none"
      });

      // Text pulse
      gsap.to(textRef.current, {
        opacity: 0.5,
        duration: 1,
        repeat: -1,
        yoyo: true,
        ease: "sine.inOut"
      });
    });

    return () => ctx.revert();
  }, []);

  return (
    <div className="flex min-h-50 items-center justify-center bg-background px-4 py-16">
      <div
        ref={containerRef}
        className="inline-flex items-center gap-4 rounded-2xl border border-border/40 bg-card/80 px-6 py-3.5 shadow-xl backdrop-blur-md"
      >
        <div className="relative flex h-5 w-5 items-center justify-center">
          <div
            ref={spinnerRef}
            className="h-full w-full rounded-full border-2 border-primary/20 border-t-primary"
          />
        </div>
        <span
          ref={textRef}
          className="text-sm font-semibold tracking-tight text-foreground"
        >
          Loading...
        </span>
      </div>
    </div>
  );
};
