import { useRef, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { DEMO_HINT } from '../../api';
import { usePictures } from '../../mutations';
import type { PictureOwnerKind, PictureResponse } from '../../types';
import { PictureLightbox } from './PictureLightbox';

export function CameraGlyph({ size = 14 }: { size?: number }) {
  return (
    <svg viewBox="0 0 24 24" width={size} height={size} aria-hidden="true">
      <path
        fill="currentColor"
        d="M9.4 4 8 6H5a2 2 0 0 0-2 2v10a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V8a2 2 0 0 0-2-2h-3l-1.4-2H9.4ZM12 8.8A4.2 4.2 0 1 1 7.8 13 4.2 4.2 0 0 1 12 8.8Zm0 2A2.2 2.2 0 1 0 14.2 13 2.2 2.2 0 0 0 12 10.8Z"
      />
    </svg>
  );
}

/**
 * The thumbnail body shared by the hover icon and the section chips: the
 * owner's cover photo (isPrimary, else the first), a hint when there are no
 * photos, and a one-shot query invalidation when the image fails to load
 * (an expired SAS URL — refetching mints a fresh one).
 */
export function PicturePreview({
  kind,
  ownerId,
  pictures,
  onOpen,
}: {
  kind: PictureOwnerKind;
  ownerId: number;
  pictures: PictureResponse[] | undefined;
  onOpen?: (picture: PictureResponse) => void;
}) {
  const qc = useQueryClient();
  const retried = useRef(false);
  if (pictures === undefined) {
    return <span className="pic-popover-hint">Loading…</span>;
  }
  const cover = pictures.find((p) => p.isPrimary) ?? pictures[0];
  if (!cover) {
    return <span className="pic-popover-hint">No photos yet</span>;
  }
  return (
    <>
      <img
        src={cover.url}
        alt={cover.caption ?? 'Photo'}
        onClick={onOpen ? () => onOpen(cover) : undefined}
        onError={() => {
          if (retried.current) return;
          retried.current = true;
          qc.invalidateQueries({ queryKey: ['pictures', kind, ownerId] });
        }}
      />
      {pictures.length > 1 && <span className="pic-popover-count">{pictures.length} photos</span>}
    </>
  );
}

/**
 * The compact "this thing may have photos" affordance used in list rows and
 * card headers: a camera glyph whose hover (or tap, on touch screens) reveals
 * the cover thumbnail in a popover. Pictures are fetched lazily on the first
 * hover so long tables never fan out into per-row requests up front.
 */
export function PictureHoverIcon({ kind, ownerId, live }: { kind: PictureOwnerKind; ownerId: number; live: boolean }) {
  const [armed, setArmed] = useState(false);
  const [open, setOpen] = useState(false);
  const [lightbox, setLightbox] = useState<PictureResponse | null>(null);
  const { data: pictures } = usePictures(kind, ownerId, live && armed);

  const arm = () => {
    if (!live) return;
    setArmed(true);
    setOpen(true);
  };

  return (
    <span className="pic-hover" onMouseEnter={arm} onMouseLeave={() => setOpen(false)}>
      <button
        type="button"
        className={live ? 'pic-icon' : 'pic-icon demo-disabled'}
        disabled={!live}
        aria-label="Photos"
        title={live ? 'Photos' : DEMO_HINT}
        onClick={(e) => {
          // rows are often clickable themselves; the icon must not trigger them
          e.stopPropagation();
          setArmed(true);
          setOpen((o) => !o);
        }}
      >
        <CameraGlyph />
      </button>
      {open && (
        <span className="pic-popover" onClick={(e) => e.stopPropagation()}>
          <PicturePreview kind={kind} ownerId={ownerId} pictures={pictures} onOpen={setLightbox} />
        </span>
      )}
      {lightbox && (
        <PictureLightbox
          kind={kind}
          ownerId={ownerId}
          pictureId={lightbox.id}
          live={live}
          onClose={() => setLightbox(null)}
        />
      )}
    </span>
  );
}
