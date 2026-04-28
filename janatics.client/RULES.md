# React Component Rules

These rules must be read and applied before making any changes to React components.

## [R-01] SIZE LIMIT
Refactor so every component file is STRICTLY under 100 lines. Extract sub-components, custom hooks, and helper functions.

## [R-02] FOLDER COLOCATION
Group all related files into a `FeatureName/` folder. Expose only `index.tsx` as the public entry point.

Layout:
- `index.tsx`
- `SubComponent.tsx`
- `hooks/`
- `utils/`
- `types.ts`

## [R-03] READABILITY ORDER
Structure every component in this exact order:

1. Types / Interfaces
2. Static constants (outside function)
3. Component signature
4. Hook calls (useState → useEffect → custom)
5. Derived / memoised values
6. Event handlers (handleXxx)
7. Early returns (loading | error | empty)
8. Single JSX return

## NAMING CONVENTIONS
- Props interface → `Props`
- Event handlers → `handleXxx`
- Boolean props → `isXxx` | `hasXxx` | `canXxx`
- Custom hooks → `useXxx`

## WHAT TO AVOID
- ✗ Inline arrow functions in JSX
- ✗ Ternaries deeper than 1 level
- ✗ Mixed logic and JSX in the same block
- ✗ Any file exceeding 100 lines

## APPLICATION
Apply these rules to the component below and output the refactored files with their full folder structure.

## Project Context
Refer this document for overall feature
[text](../Docs/Janatics_DataEngine_SystemDesign.docx)
[text](../Docs/Janatics_DataEngine_Complete.pdf)