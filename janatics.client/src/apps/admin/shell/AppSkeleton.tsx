import { Skeleton } from "@/components/ui/skeleton";

export default function AppSkeleton() {
  return (
    <div className="min-h-screen bg-background p-6">
      <div className="grid gap-6 lg:grid-cols-[280px_1fr]">
        <Skeleton className="h-[80vh] rounded-[28px]" />
        <div className="space-y-6">
          <Skeleton className="h-24 rounded-[28px]" />
          <div className="grid gap-6 md:grid-cols-3">
            <Skeleton className="h-32 rounded-3xl" />
            <Skeleton className="h-32 rounded-3xl" />
            <Skeleton className="h-32 rounded-3xl" />
          </div>
          <Skeleton className="h-[48vh] rounded-[28px]" />
        </div>
      </div>
    </div>
  );
}
