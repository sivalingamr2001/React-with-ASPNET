import PageFrame from "../../shell/components/PageFrame";
import type { AdminPageConfig } from "../../shell/types";
import { useFieldMapper } from "./hooks/useFieldMapper";
import MapperCanvas from "./components/MapperCanvas";
import MapperTabs from "./components/MapperTabs";
import MapperToolbar from "./components/MapperToolbar";
import PayloadPreview from "./components/PayloadPreview";

interface Props {
  page: AdminPageConfig;
}

export default function FieldMapperPage({ page }: Props) {
  const state = useFieldMapper();
  const handleEntityChange = (value: string) => {
    state.setSelectedEntity(value);
    state.setFieldValues({});
    state.setChildren([]);
    state.setTransactionId("");
  };
  const handleAddChild = (entity: string) =>
    state.setChildren((items) => [
      ...items,
      {
        entity,
        fkColumn: `${state.selectedEntity}Id`,
        id: Date.now(),
        values: {},
      },
    ]);
  const handleFieldChange = (name: string, value: string) =>
    state.setFieldValues((items) => ({ ...items, [name]: value }));
  const handleChildFKChange = (id: number, value: string) =>
    state.setChildren((items) =>
      items.map((item) =>
        item.id === id ? { ...item, fkColumn: value } : item,
      ),
    );
  const handleChildRemove = (id: number) =>
    state.setChildren((items) => items.filter((item) => item.id !== id));
  const handleChildValueChange = (id: number, column: string, value: string) =>
    state.setChildren((items) =>
      items.map((item) =>
        item.id === id
          ? { ...item, values: { ...item.values, [column]: value } }
          : item,
      ),
    );

  return (
    <PageFrame description={page.description} title={page.title}>
      <MapperToolbar
        isUpdate={state.isUpdate}
        onEntityChange={handleEntityChange}
        onModeChange={state.setIsUpdate}
        onTransactionChange={state.setTransactionId}
        selectedEntity={state.selectedEntity}
        transactionId={state.transactionId}
      />
      <MapperTabs
        activeTab={state.activeTab}
        onTabChange={state.setActiveTab}
      />
      {state.schema && state.activeTab === "mapper" && (
        <MapperCanvas
          availableChildren={state.schema.children}
          children={state.children}
          columns={state.schema.columns}
          mappedCount={state.mappedCount}
          missingRequired={state.missingRequired}
          onAddChild={handleAddChild}
          onChildFKChange={handleChildFKChange}
          onChildRemove={handleChildRemove}
          onChildValueChange={handleChildValueChange}
          onFieldChange={handleFieldChange}
          values={state.fieldValues}
        />
      )}
      {state.payload && state.activeTab === "preview" && (
        <PayloadPreview canSubmit={state.isValid} preview={state.preview} />
      )}
    </PageFrame>
  );
}
