import QueryBuilderPage from "./features/query-builder";

const page = {
  id: "query-builder",
  route: "/resources/query-builder",
  title: "Query Builder Studio",
  description:
    "Enterprise query builder with reusable panels and preview tooling.",
  icon: "Filter",
  breadcrumb: ["Admin", "Resources", "Query Builder"],
};

export default function QueryBuilder() {
  return <QueryBuilderPage page={page} />;
}
