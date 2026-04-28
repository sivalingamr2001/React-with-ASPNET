import { Copy } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

interface Props {
  preview: string;
}

export default function ResultsPreview({ preview }: Props) {
  const handleCopy = () => navigator.clipboard?.writeText(preview);

  return (
    <Card className="rounded-[28px] border-white/60 bg-slate-950 text-slate-50">
      <CardHeader className="flex flex-row items-center justify-between">
        <CardTitle>Query Payload</CardTitle>
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
      <CardContent>
        <pre className="overflow-x-auto rounded-[24px] bg-white/5 p-4 text-xs leading-6">
          {preview}
        </pre>
      </CardContent>
    </Card>
  );
}
