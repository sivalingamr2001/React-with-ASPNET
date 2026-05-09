import { Controller, type Control } from "react-hook-form";
import * as React from "react";

export interface FormProps extends React.FormHTMLAttributes<HTMLFormElement> {
  children: React.ReactNode;
  [key: string]: unknown;
}

export function Form({ children, ...props }: FormProps) {
  return <form {...(props as React.FormHTMLAttributes<HTMLFormElement>)}>{children}</form>;
}

export interface FormFieldProps {
  control: Control<any, any>;
  name: any;
  render: (params: any) => React.ReactElement;
}

export function FormField({ control, name, render }: FormFieldProps) {
  return <Controller control={control} name={name} render={render} />;
}

export function FormItem(props: React.HTMLAttributes<HTMLDivElement>) {
  return <div {...props} />;
}

export function FormLabel(props: React.LabelHTMLAttributes<HTMLLabelElement>) {
  return <label {...props} />;
}

export function FormControl(props: React.HTMLAttributes<HTMLDivElement>) {
  return <div {...props} />;
}

export function FormDescription(props: React.HTMLAttributes<HTMLParagraphElement>) {
  return <p {...props} />;
}

export function FormMessage(props: React.HTMLAttributes<HTMLParagraphElement>) {
  return <p {...props} />;
}
