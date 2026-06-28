import { Controller, type FieldErrors } from 'react-hook-form';

// Small Controller-based form controls shared by the management forms. They own
// the fiddly conversions: empty text → null for nullable fields, empty number
// inputs → null, and <select> string values → numeric ids/enum ordinals.
//
// `control` is intentionally untyped: react-hook-form's Control is invariant, so
// a typed Control<ItemForm> won't assign to a shared Control<FieldValues> prop.
// These wrappers are tiny and the field names are checked by usage, so we accept
// the looseness here rather than make every wrapper generic.

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type AnyControl = any;

function fieldError(errors: FieldErrors, name: string): string | undefined {
  const e = errors[name];
  return e?.message as string | undefined;
}

interface BaseProps {
  control: AnyControl;
  errors: FieldErrors;
  name: string;
  label: string;
}

export function TextField({ control, errors, name, label, nullable, placeholder }: BaseProps & { nullable?: boolean; placeholder?: string }) {
  return (
    <label className="form-field">
      <span>{label}</span>
      <Controller
        control={control}
        name={name}
        render={({ field }) => (
          <input
            type="text"
            value={(field.value as string | null) ?? ''}
            placeholder={placeholder}
            onChange={(e) => field.onChange(nullable && e.target.value === '' ? null : e.target.value)}
          />
        )}
      />
      {fieldError(errors, name) && <em className="form-error">{fieldError(errors, name)}</em>}
    </label>
  );
}

export function TextAreaField({ control, errors, name, label, nullable }: BaseProps & { nullable?: boolean }) {
  return (
    <label className="form-field">
      <span>{label}</span>
      <Controller
        control={control}
        name={name}
        render={({ field }) => (
          <textarea
            rows={2}
            value={(field.value as string | null) ?? ''}
            onChange={(e) => field.onChange(nullable && e.target.value === '' ? null : e.target.value)}
          />
        )}
      />
      {fieldError(errors, name) && <em className="form-error">{fieldError(errors, name)}</em>}
    </label>
  );
}

export function NumberField({
  control,
  errors,
  name,
  label,
  integer,
  step,
}: BaseProps & { integer?: boolean; step?: number }) {
  return (
    <label className="form-field">
      <span>{label}</span>
      <Controller
        control={control}
        name={name}
        render={({ field }) => (
          <input
            type="number"
            step={step ?? (integer ? 1 : 0.01)}
            value={field.value === null || field.value === undefined ? '' : (field.value as number)}
            onChange={(e) => {
              const v = e.target.value;
              if (v === '') return field.onChange(null);
              field.onChange(integer ? parseInt(v, 10) : parseFloat(v));
            }}
          />
        )}
      />
      {fieldError(errors, name) && <em className="form-error">{fieldError(errors, name)}</em>}
    </label>
  );
}

export function DateField({ control, errors, name, label }: BaseProps) {
  return (
    <label className="form-field">
      <span>{label}</span>
      <Controller
        control={control}
        name={name}
        render={({ field }) => (
          <input
            type="date"
            value={(field.value as string | null)?.slice(0, 10) ?? ''}
            onChange={(e) => field.onChange(e.target.value === '' ? null : e.target.value)}
          />
        )}
      />
      {fieldError(errors, name) && <em className="form-error">{fieldError(errors, name)}</em>}
    </label>
  );
}

export function CheckboxField({ control, name, label }: Omit<BaseProps, 'errors'>) {
  return (
    <label className="form-field form-field-inline">
      <Controller
        control={control}
        name={name}
        render={({ field }) => (
          <input type="checkbox" checked={!!field.value} onChange={(e) => field.onChange(e.target.checked)} />
        )}
      />
      <span>{label}</span>
    </label>
  );
}

export interface Option {
  value: number;
  label: string;
}

export function SelectField({
  control,
  errors,
  name,
  label,
  options,
  placeholder,
  required,
}: BaseProps & { options: Option[]; placeholder?: string; required?: boolean }) {
  return (
    <label className="form-field">
      <span>{label}</span>
      <Controller
        control={control}
        name={name}
        render={({ field }) => (
          <select
            value={field.value === null || field.value === undefined ? '' : String(field.value)}
            onChange={(e) => field.onChange(e.target.value === '' ? (required ? 0 : null) : Number(e.target.value))}
          >
            <option value="">{placeholder ?? '—'}</option>
            {options.map((o) => (
              <option key={o.value} value={o.value}>
                {o.label}
              </option>
            ))}
          </select>
        )}
      />
      {fieldError(errors, name) && <em className="form-error">{fieldError(errors, name)}</em>}
    </label>
  );
}

/** Multi-select chip row for a number[] field (item types). */
export function ChipMultiField({
  control,
  errors,
  name,
  label,
  options,
}: BaseProps & { options: Option[] }) {
  return (
    <div className="form-field">
      <span>{label}</span>
      <Controller
        control={control}
        name={name}
        render={({ field }) => {
          const selected = (field.value as number[]) ?? [];
          const toggle = (v: number) =>
            field.onChange(selected.includes(v) ? selected.filter((x) => x !== v) : [...selected, v]);
          return (
            <div className="form-chips">
              {options.map((o) => (
                <button
                  type="button"
                  key={o.value}
                  className={`form-chip${selected.includes(o.value) ? ' on' : ''}`}
                  onClick={() => toggle(o.value)}
                >
                  {o.label}
                </button>
              ))}
            </div>
          );
        }}
      />
      {fieldError(errors, name) && <em className="form-error">{fieldError(errors, name)}</em>}
    </div>
  );
}
