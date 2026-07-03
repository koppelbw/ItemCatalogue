import { useState, type ReactNode } from 'react';

const DEFAULT_PAGE_SIZE = 10;

/**
 * Client-side pagination wrapper: hands the current page's rows to `children`
 * and renders prev/next controls underneath when there is more than one page.
 * The page clamps rather than resets when rows shrink (e.g. after a delete);
 * key the component by view when a hard reset is wanted.
 */
export function Paginated<T>({
  rows,
  pageSize = DEFAULT_PAGE_SIZE,
  children,
}: {
  rows: T[];
  pageSize?: number;
  children: (pageRows: T[]) => ReactNode;
}) {
  const [page, setPage] = useState(1);
  const totalPages = Math.max(1, Math.ceil(rows.length / pageSize));
  const current = Math.min(page, totalPages);
  const start = (current - 1) * pageSize;

  return (
    <>
      {children(rows.slice(start, start + pageSize))}
      {totalPages > 1 && (
        <div className="pager">
          <button className="btn btn-small" disabled={current === 1} onClick={() => setPage(current - 1)}>
            ‹ Prev
          </button>
          <span className="pager-status">
            {start + 1}–{Math.min(start + pageSize, rows.length)} of {rows.length}
          </span>
          <button className="btn btn-small" disabled={current === totalPages} onClick={() => setPage(current + 1)}>
            Next ›
          </button>
        </div>
      )}
    </>
  );
}
