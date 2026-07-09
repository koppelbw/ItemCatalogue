import { useEffect, useRef, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { importTemplateUrl } from '../api';
import { useImportJob, useImportJobs, useStartImport } from '../imports';
import { mapApiError } from '../mutations';
import { IMPORT_STATUS_COMPLETED, IMPORT_STATUS_NAMES, type ImportJobResponse } from '../types';
import { Paginated } from './Paginated';

/**
 * The Manage › Import tab: upload a CSV of items, watch the background job chew
 * through it (chunks of 25 via the Storage Queue + Azure Function), and browse
 * past jobs. Intake rejections (parse errors, bad reference ids) appear on the
 * job immediately; chunk-time failures stream in as the poller refreshes.
 */
export function ImportSection({ live }: { live: boolean }) {
  const qc = useQueryClient();
  const [file, setFile] = useState<File | null>(null);
  const [banner, setBanner] = useState<string | null>(null);
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [page, setPage] = useState(1);
  const fileInput = useRef<HTMLInputElement>(null);

  const start = useStartImport();
  const jobsQuery = useImportJobs(page, live);
  const { data: job } = useImportJob(live ? selectedId : null);

  // When a job we are watching crosses into Completed, its items just became
  // real rows — refresh the catalogue (Items tab, 3D scene) and the history list.
  const lastSeen = useRef<{ id: number; status: number } | null>(null);
  useEffect(() => {
    if (!job) return;
    const previous = lastSeen.current;
    if (
      previous &&
      previous.id === job.id &&
      previous.status !== IMPORT_STATUS_COMPLETED &&
      job.status === IMPORT_STATUS_COMPLETED
    ) {
      void qc.invalidateQueries({ queryKey: ['catalogue'] });
      void qc.invalidateQueries({ queryKey: ['importJobs'] });
    }
    lastSeen.current = { id: job.id, status: job.status };
  }, [job, qc]);

  const upload = async () => {
    if (!file) return;
    setBanner(null);
    try {
      const created = await start.mutateAsync(file);
      setSelectedId(created.id);
      setFile(null);
      if (fileInput.current) fileInput.current.value = '';
    } catch (e) {
      setBanner(mapApiError(e).banner);
    }
  };

  const jobs = jobsQuery.data;

  return (
    <>
      <section className="manage-section">
        <div className="manage-section-head">
          <h2>Bulk import</h2>
          <a className="btn btn-small" href={importTemplateUrl}>
            Download template
          </a>
        </div>
        <p className="form-hint">
          Upload a CSV of items (up to 1,000 rows); they are inserted in the background in chunks of 25.
          Reference columns (RoomId, ContainerId, OwnerId) take numeric ids from the tables above, dates are
          yyyy-mm-dd, and multiple ItemTypes are separated with “;”.
        </p>
        {banner && <p className="form-banner">{banner}</p>}
        <div className="import-controls">
          <input
            ref={fileInput}
            type="file"
            accept=".csv,text/csv"
            aria-label="CSV file to import"
            disabled={!live}
            onChange={(e) => setFile(e.target.files?.[0] ?? null)}
          />
          <button className="btn btn-primary" disabled={!live || !file || start.isPending} onClick={() => void upload()}>
            {start.isPending ? 'Uploading…' : 'Upload'}
          </button>
        </div>
      </section>

      {job && <JobPanel job={job} />}

      <section className="manage-section">
        <div className="manage-section-head">
          <h2>Import history</h2>
        </div>
        {!live ? (
          <p className="explore-empty">Import history is unavailable while showing demo data.</p>
        ) : !jobs || jobs.items.length === 0 ? (
          <p className="explore-empty">No imports yet.</p>
        ) : (
          <>
            <table className="manage-table">
              <thead>
                <tr>
                  <th>#</th><th>File</th><th>Uploaded</th><th>Status</th><th>Rows</th><th>Succeeded</th><th>Failed</th><th></th>
                </tr>
              </thead>
              <tbody>
                {jobs.items.map((j) => (
                  <tr key={j.id} className={j.id === selectedId ? 'row-selected' : ''}>
                    <td>{j.id}</td>
                    <td>{j.fileName}</td>
                    <td>{formatWhen(j.createdDate)}</td>
                    <td><StatusBadge status={j.status} /></td>
                    <td>{j.totalRows}</td>
                    <td>{j.succeeded}</td>
                    <td>{j.failed}</td>
                    <td className="row-actions">
                      <button className="btn btn-small" onClick={() => setSelectedId(j.id)}>
                        View
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            {(jobs.hasPrevious || jobs.hasNext) && (
              <div className="pager">
                <button className="btn btn-small" disabled={!jobs.hasPrevious} onClick={() => setPage(page - 1)}>
                  ‹ Prev
                </button>
                <span className="pager-status">
                  page {jobs.page} of {jobs.totalPages}
                </span>
                <button className="btn btn-small" disabled={!jobs.hasNext} onClick={() => setPage(page + 1)}>
                  Next ›
                </button>
              </div>
            )}
          </>
        )}
      </section>
    </>
  );
}

/** Progress + counts + per-row errors for the selected (or just-uploaded) job. */
function JobPanel({ job }: { job: ImportJobResponse }) {
  const done = job.status === IMPORT_STATUS_COMPLETED;
  const percent = job.totalChunks === 0 ? 100 : Math.round((job.processedChunks / job.totalChunks) * 100);

  return (
    <section className="manage-section">
      <div className="manage-section-head">
        <h2>
          Job #{job.id} — {job.fileName}
        </h2>
        <StatusBadge status={job.status} />
      </div>

      <div className="import-progress" role="progressbar" aria-valuenow={percent} aria-valuemin={0} aria-valuemax={100}>
        <div className="import-progress-fill" style={{ width: `${percent}%` }} />
      </div>
      <p className="import-progress-label">
        {job.processedChunks} of {job.totalChunks} chunk{job.totalChunks === 1 ? '' : 's'} processed
      </p>

      <div className="import-stats">
        <Stat label="Rows" value={job.totalRows} />
        <Stat label="Enqueued" value={job.enqueuedRows} />
        <Stat label="Succeeded" value={job.succeeded} />
        <Stat label="Failed" value={job.failed} />
        <Stat label="Rejected at intake" value={job.rejectedAtIntake} />
      </div>

      {!done && <p className="form-hint">Rows are processed in the background — this panel refreshes automatically.</p>}

      {job.errors.length > 0 && (
        <>
          <h3 className="import-errors-title">Row errors</h3>
          <Paginated rows={job.errors}>
            {(rows) => (
              <table className="manage-table">
                <thead>
                  <tr>
                    <th>CSV row</th>
                    <th>Problems</th>
                  </tr>
                </thead>
                <tbody>
                  {rows.map((e, i) => (
                    <tr key={`${e.rowNumber}-${i}`}>
                      {/* row 0 = a whole-file problem (e.g. a poisoned chunk with an unreadable payload) */}
                      <td>{e.rowNumber === 0 ? 'file' : e.rowNumber}</td>
                      <td>{e.messages.join(' ')}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </Paginated>
        </>
      )}
    </section>
  );
}

function StatusBadge({ status }: { status: number }) {
  return <span className={`import-status s${status}`}>{IMPORT_STATUS_NAMES[status] ?? `status ${status}`}</span>;
}

function Stat({ label, value }: { label: string; value: number }) {
  return (
    <div className="import-stat">
      <b>{value}</b>
      <span>{label}</span>
    </div>
  );
}

/** API DateTimes are UTC but serialized without an offset; pin them before localizing. */
function formatWhen(iso: string): string {
  const utc = iso.endsWith('Z') || iso.includes('+') ? iso : `${iso}Z`;
  return new Date(utc).toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' });
}
