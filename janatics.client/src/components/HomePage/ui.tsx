import React from "react";
import { PROVIDER_COLORS, STATUS_COLORS } from "./data";

export function ProviderBadge({ provider }: { provider: string }) {
  const c = PROVIDER_COLORS[provider] || {
    bg: "#f0f0f0",
    text: "#444",
    border: "#ccc",
  };
  return (
    <span
      style={{
        fontSize: 10,
        fontFamily: "'JetBrains Mono',monospace",
        fontWeight: 700,
        background: c.bg,
        color: c.text,
        border: `1px solid ${c.border}`,
        padding: "2px 8px",
        borderRadius: 4,
        letterSpacing: "0.05em",
      }}
    >
      {provider}
    </span>
  );
}

export function StatusDot({ status }: { status: string }) {
  const s = STATUS_COLORS[status] || STATUS_COLORS.idle;
  return (
    <span style={{ display: "flex", alignItems: "center", gap: 5 }}>
      <span
        style={{
          width: 7,
          height: 7,
          borderRadius: "50%",
          background: s.dot,
          boxShadow: status === "connected" ? `0 0 0 2px ${s.dot}33` : "none",
          display: "inline-block",
        }}
      />
      <span style={{ fontSize: 11, color: s.text, fontWeight: 600 }}>
        {status}
      </span>
    </span>
  );
}

export function Tag({
  label,
  onRemove,
}: {
  label: string;
  onRemove?: () => void;
}) {
  return (
    <span
      style={{
        display: "inline-flex",
        alignItems: "center",
        gap: 4,
        background: "#eef2ff",
        color: "#3730a3",
        border: "1px solid #c7d2fe",
        borderRadius: 4,
        padding: "2px 8px",
        fontSize: 11,
        fontWeight: 600,
      }}
    >
      {label}
      {onRemove && (
        <span style={{ cursor: "pointer", opacity: 0.6 }} onClick={onRemove}>
          ×
        </span>
      )}
    </span>
  );
}

export function Modal({
  title,
  onClose,
  children,
}: {
  title: string;
  onClose: () => void;
  children: React.ReactNode;
}) {
  return (
    <div
      style={{
        position: "fixed",
        inset: 0,
        background: "rgba(10,18,40,0.55)",
        backdropFilter: "blur(3px)",
        zIndex: 1000,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        padding: 20,
      }}
    >
      <div
        style={{
          background: "#fff",
          borderRadius: 14,
          width: "100%",
          maxWidth: 560,
          boxShadow: "0 24px 80px rgba(10,18,60,0.22)",
          maxHeight: "90vh",
          overflow: "auto",
        }}
      >
        <div
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            padding: "18px 24px",
            borderBottom: "1px solid #f0f4f8",
          }}
        >
          <span
            style={{
              fontWeight: 700,
              fontSize: 15,
              color: "#0f1c3a",
              fontFamily: "'Sora',sans-serif",
            }}
          >
            {title}
          </span>
          <button
            onClick={onClose}
            style={{
              background: "#f4f6f8",
              border: "none",
              borderRadius: 6,
              width: 28,
              height: 28,
              cursor: "pointer",
              fontSize: 16,
              color: "#606878",
            }}
          >
            ×
          </button>
        </div>
        <div style={{ padding: "20px 24px" }}>{children}</div>
      </div>
    </div>
  );
}

export function Field({
  label,
  children,
  hint,
}: {
  label: string;
  children: React.ReactNode;
  hint?: React.ReactNode;
}) {
  return (
    <div style={{ marginBottom: 16 }}>
      <label
        style={{
          display: "block",
          fontSize: 11,
          fontWeight: 700,
          color: "#64748b",
          letterSpacing: "0.07em",
          textTransform: "uppercase",
          marginBottom: 6,
          fontFamily: "'Sora',sans-serif",
        }}
      >
        {label}
      </label>
      {children}
      {hint && (
        <div style={{ fontSize: 11, color: "#94a3b8", marginTop: 4 }}>
          {hint}
        </div>
      )}
    </div>
  );
}

export const inputSx = {
  width: "100%",
  padding: "9px 12px",
  border: "1.5px solid #e2e8f0",
  borderRadius: 7,
  fontSize: 13,
  color: "#0f1c3a",
  background: "#f8faff",
  fontFamily: "'DM Mono',monospace",
  outline: "none",
  boxSizing: "border-box" as const,
};
export const selectSx = {
  ...inputSx,
  fontFamily: "'Sora',sans-serif",
  cursor: "pointer",
};
