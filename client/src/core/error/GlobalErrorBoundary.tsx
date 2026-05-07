import { Component, type ReactNode, type ErrorInfo } from "react";
import { Button } from "@/shared/components/ui/button";

interface Props {
  children: ReactNode;
  fallback?: ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
  errorId: string | null;
}

export class GlobalErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false, error: null, errorId: null };
  }

  static getDerivedStateFromError(error: Error): State {
    const errorId = `err_${Date.now()}_${Math.random().toString(36).slice(2)}`;
    return { hasError: true, error, errorId };
  }

  componentDidCatch(error: Error, info: ErrorInfo): void {
    // Send to monitoring service (e.g., Sentry)
    console.error("[GlobalErrorBoundary]", {
      error,
      componentStack: info.componentStack,
      errorId: this.state.errorId,
    });

    // In production: Sentry.captureException(error, { extra: { errorId, componentStack } })
  }

  handleReset = (): void => {
    this.setState({ hasError: false, error: null, errorId: null });
  };

  render(): ReactNode {
    if (this.state.hasError) {
      if (this.props.fallback) return this.props.fallback;

      return (
        <div className="flex min-h-screen flex-col items-center justify-center gap-6 p-8 text-center">
          <div className="space-y-2">
            <h1 className="text-3xl font-bold tracking-tight">
              Something went wrong
            </h1>
            <p className="text-muted-foreground">
              An unexpected error occurred. Our team has been notified.
            </p>
            {this.state.errorId && (
              <p className="font-mono text-xs text-muted-foreground">
                Error ID: {this.state.errorId}
              </p>
            )}
          </div>
          <div className="flex gap-3">
            <Button onClick={this.handleReset}>Try Again</Button>
            <Button variant="outline" onClick={() => window.location.assign("/")}>
              Return Home
            </Button>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}