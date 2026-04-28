import { Button } from "@/components/ui/button";

interface Props {
  activeTab: string;
  onTabChange: (value: string) => void;
}

export default function MapperTabs({ activeTab, onTabChange }: Props) {
  return (
    <div className="flex gap-2">
      <Button
        className="rounded-full px-4"
        onClick={onTabChange.bind(null, "mapper")}
        variant={activeTab === "mapper" ? "default" : "outline"}
      >
        Mapper
      </Button>
      <Button
        className="rounded-full px-4"
        onClick={onTabChange.bind(null, "preview")}
        variant={activeTab === "preview" ? "default" : "outline"}
      >
        Payload Preview
      </Button>
    </div>
  );
}
