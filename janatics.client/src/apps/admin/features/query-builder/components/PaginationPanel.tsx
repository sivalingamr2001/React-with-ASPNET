import { Switch } from "@/components/ui/switch";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";

interface Props {
  includeCount: boolean;
  page: number;
  pageSize: number;
  onIncludeCountChange: (value: boolean) => void;
  onPageChange: (value: number) => void;
  onPageSizeChange: (value: number) => void;
}

export default function PaginationPanel(props: Props) {
  const handlePage = (event: React.ChangeEvent<HTMLInputElement>) =>
    props.onPageChange(Number(event.target.value) || 1);
  const handlePageSize = (event: React.ChangeEvent<HTMLInputElement>) =>
    props.onPageSizeChange(Number(event.target.value) || 25);

  return (
    <Card className="rounded-[28px] border-white/60 bg-card/85">
      <CardHeader>
        <CardTitle>Pagination</CardTitle>
      </CardHeader>
      <CardContent className="grid gap-4 md:grid-cols-3">
        <Input
          className="h-11 rounded-2xl"
          onChange={handlePage}
          placeholder="Page"
          type="number"
          value={props.page}
        />
        <Input
          className="h-11 rounded-2xl"
          onChange={handlePageSize}
          placeholder="Page Size"
          type="number"
          value={props.pageSize}
        />
        <label className="flex items-center justify-between rounded-2xl border border-white/60 bg-background/70 px-4">
          <span className="text-sm">Include count</span>
          <Switch
            checked={props.includeCount}
            onCheckedChange={props.onIncludeCountChange}
          />
        </label>
      </CardContent>
    </Card>
  );
}
