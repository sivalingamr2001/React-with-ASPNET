import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb";

interface Props {
  items: string[];
}

export default function BreadcrumbTrail({ items }: Props) {
  return (
    <Breadcrumb>
      <BreadcrumbList>
        {items.map((item, index) => (
          <BreadcrumbNode
            item={item}
            key={`${item}-${index}`}
            isLast={index === items.length - 1}
          />
        ))}
      </BreadcrumbList>
    </Breadcrumb>
  );
}

function BreadcrumbNode({ item, isLast }: { isLast: boolean; item: string }) {
  if (isLast) {
    return (
      <BreadcrumbItem>
        <BreadcrumbPage>{item}</BreadcrumbPage>
      </BreadcrumbItem>
    );
  }

  return (
    <>
      <BreadcrumbItem>{item}</BreadcrumbItem>
      <BreadcrumbSeparator />
    </>
  );
}
