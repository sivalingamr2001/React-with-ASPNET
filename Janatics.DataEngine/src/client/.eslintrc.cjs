/** @type {import('eslint').Linter.Config} */
module.exports = {
  root: true,
  env: { browser: true, es2022: true },
  extends: [
    "eslint:recommended",
    "plugin:@typescript-eslint/strict-type-checked",
    "plugin:react/recommended",
    "plugin:react/jsx-runtime",
    "plugin:react-hooks/recommended",
    "plugin:jsx-a11y/recommended",
    "plugin:import/recommended",
    "plugin:import/typescript",
  ],
  parser: "@typescript-eslint/parser",
  parserOptions: {
    ecmaVersion: "latest",
    sourceType: "module",
    project: ["./tsconfig.json", "./tsconfig.node.json"],
    tsconfigRootDir: __dirname,
  },
  plugins: [
    "@typescript-eslint",
    "react-refresh",
    "react",
    "jsx-a11y",
    "import",
  ],
  settings: {
    react: { version: "detect" },
    "import/resolver": {
      typescript: { alwaysTryTypes: true, project: "./tsconfig.json" },
    },
  },
  rules: {
    // React
    "react-refresh/only-export-components": [
      "warn",
      { allowConstantExport: true },
    ],
    "react/prop-types": "off", // TypeScript handles this

    // TypeScript
    "@typescript-eslint/no-explicit-any": "error",
    "@typescript-eslint/no-unused-vars": [
      "error",
      { argsIgnorePattern: "^_", varsIgnorePattern: "^_" },
    ],
    "@typescript-eslint/consistent-type-imports": [
      "error",
      { prefer: "type-imports", fixStyle: "inline-type-imports" },
    ],
    "@typescript-eslint/no-floating-promises": "error",
    "@typescript-eslint/await-thenable": "error",

    // Imports
    "import/order": [
      "error",
      {
        groups: [
          "builtin",
          "external",
          "internal",
          ["parent", "sibling", "index"],
        ],
        "newlines-between": "always",
        alphabetize: { order: "asc", caseInsensitive: true },
      },
    ],
    "import/no-default-export": "off", // Pages and layouts use default exports

    // General
    "no-console": ["warn", { allow: ["warn", "error"] }],
    eqeqeq: ["error", "always"],
  },
  overrides: [
    {
      // Relax rules for config files
      files: ["*.config.ts", "*.config.cjs", "vite.config.ts"],
      rules: {
        "import/no-default-export": "off",
        "@typescript-eslint/no-var-requires": "off",
      },
    },
  ],
};