# React + TypeScript + Vite + shadcn/ui

This is a template for a new Vite project with React, TypeScript, and shadcn/ui.

## Adding components

To add components to your app, run the following command:

```bash
npx shadcn@latest add button
```

This will place the ui components in the `src/components` directory.

## Using components

To use the components in your app, import them as follows:

```tsx
import { Button } from "@/components/ui/button"
```
To see your new structure in the terminal, run:tree /f src


cd "d:\New Workspace\React with ASPNET\react-app"; Get-ChildItem src -Recurse -File -Include *.ts,*.tsx | Sort-Object FullName | ForEach-Object { "$($_.FullName) - $($_.Length) bytes" }