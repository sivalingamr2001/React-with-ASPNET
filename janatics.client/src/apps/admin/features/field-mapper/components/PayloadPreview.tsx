import { Copy } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

interface Props {
  canSubmit: boolean;
  preview: string;
}

export default function PayloadPreview({ preview, canSubmit }: Props) {
  const handleCopy = () => navigator.clipboard?.writeText(preview);

  return (
    <Card className="rounded-[28px] border-white/60 bg-slate-950 text-slate-50">
      <CardHeader className="flex flex-row items-center justify-between">
        <CardTitle>Payload Preview</CardTitle>
        <Button
          className="rounded-full"
          onClick={handleCopy}
          size="sm"
          variant="secondary"
        >
          <Copy className="size-4" />
          Copy
        </Button>
      </CardHeader>
      <CardContent className="space-y-3">
        <pre className="overflow-x-auto rounded-[24px] bg-white/5 p-4 text-xs leading-6">
          {preview}
        </pre>
        <p className="text-sm text-slate-300">
          {canSubmit
            ? "Payload is ready for submission."
            : "Complete required fields to enable submission."}
        </p>
      </CardContent>
    </Card>
  );
}
