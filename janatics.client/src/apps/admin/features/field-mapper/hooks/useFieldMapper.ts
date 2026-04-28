import { useMemo, useState } from "react";

import { FIELD_SCHEMA } from "../data";
import { buildFieldPayload } from "../utils/buildPayload";
import type { ChildRecord } from "../types";

export function useFieldMapper() {
  const [selectedEntity, setSelectedEntity] = useState("");
  const [transactionId, setTransactionId] = useState("");
  const [isUpdate, setIsUpdate] = useState(false);
  const [fieldValues, setFieldValues] = useState<Record<string, string>>({});
  const [children, setChildren] = useState<ChildRecord[]>([]);
  const [activeTab, setActiveTab] = useState("mapper");
  const schema = selectedEntity ? FIELD_SCHEMA[selectedEntity] : null;
  const payload = selectedEntity
    ? buildFieldPayload(
        selectedEntity,
        isUpdate ? transactionId : "",
        fieldValues,
        children,
      )
    : null;
  const missingRequired =
    schema?.columns.filter((col) => col.required && !fieldValues[col.name]) ??
    [];
  const mappedCount = Object.values(fieldValues).filter(Boolean).length;
  const isValid =
    !!selectedEntity &&
    (!isUpdate || !!transactionId) &&
    missingRequired.length === 0;
  const preview = useMemo(() => JSON.stringify(payload, null, 2), [payload]);
  return {
    activeTab,
    children,
    fieldValues,
    isUpdate,
    isValid,
    mappedCount,
    missingRequired,
    payload,
    preview,
    schema,
    selectedEntity,
    setActiveTab,
    setChildren,
    setFieldValues,
    setIsUpdate,
    setSelectedEntity,
    setTransactionId,
    transactionId,
  };
}
