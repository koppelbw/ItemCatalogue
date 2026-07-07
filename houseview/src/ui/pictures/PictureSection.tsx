import { useMemo, useRef, useState, type ChangeEvent } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { DEMO_HINT } from '../../api';
import { mapApiError, usePictures, useUploadPicture } from '../../mutations';
import type { PictureOwnerKind, PictureResponse } from '../../types';
import { CameraGlyph } from './PictureHoverIcon';
import { PictureLightbox } from './PictureLightbox';

/**
 * One photo as a compact chip: camera glyph (+ star when it is the cover),
 * hover reveals the thumbnail, click opens the lightbox. Keeps the panel
 * text-light — image bytes only load when a chip is actually hovered.
 */
function PictureChip({
  kind,
  ownerId,
  picture,
  onOpen,
}: {
  kind: PictureOwnerKind;
  ownerId: number;
  picture: PictureResponse;
  onOpen: () => void;
}) {
  const [hover, setHover] = useState(false);
  const qc = useQueryClient();
  const retried = useRef(false);
  return (
    <span className="pic-hover" onMouseEnter={() => setHover(true)} onMouseLeave={() => setHover(false)}>
      <button
        type="button"
        className="pic-icon pic-chip"
        title={picture.caption ?? picture.originalFileName ?? 'Photo'}
        onClick={(e) => {
          e.stopPropagation();
          onOpen();
        }}
      >
        <CameraGlyph />
        {picture.isPrimary && <span className="pic-star">★</span>}
      </button>
      {hover && (
        <span className="pic-popover">
          <img
            src={picture.url}
            alt={picture.caption ?? 'Photo'}
            onError={() => {
              if (retried.current) return;
              retried.current = true;
              qc.invalidateQueries({ queryKey: ['pictures', kind, ownerId] });
            }}
          />
          {picture.caption && <span className="pic-popover-count">{picture.caption}</span>}
        </span>
      )}
    </span>
  );
}

/**
 * The reusable upload/manage block embedded in the 3D DetailPanel cards and
 * the Manage page's Explorer views. "Add photo" opens the OS file picker;
 * "Take photo" (touch devices only) opens the camera via the capture
 * attribute. Uploads fire immediately on pick — downscaled client-side first
 * (see imageResize.ts) — and the first photo becomes the owner's cover.
 * In demo mode the section renders greyed-out with a hover hint (the shared
 * demo-disabled pattern) so the feature stays discoverable without a live API.
 */
export function PictureSection({ kind, ownerId, live }: { kind: PictureOwnerKind; ownerId: number; live: boolean }) {
  const { data: pictures } = usePictures(kind, ownerId, live);
  const upload = useUploadPicture();
  const fileRef = useRef<HTMLInputElement>(null);
  const cameraRef = useRef<HTMLInputElement>(null);
  const [error, setError] = useState<string | null>(null);
  const [lightboxId, setLightboxId] = useState<number | null>(null);
  // capture="environment" only means something where there is a camera; on
  // desktop the button would be a duplicate file picker, so show it on touch.
  const coarsePointer = useMemo(() => window.matchMedia('(pointer: coarse)').matches, []);

  const onPick = (e: ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = ''; // allow re-picking the same file after a failure
    if (!file) return;
    setError(null);
    upload.mutate(
      { kind, ownerId, file, isPrimary: (pictures ?? []).length === 0 },
      { onError: (err) => setError(mapApiError(err).banner) },
    );
  };

  return (
    <div className="pic-section">
      <div className="panel-section-label">Photos{pictures && pictures.length > 0 ? ` (${pictures.length})` : ''}</div>
      <div className="pic-row">
        {(pictures ?? []).map((p) => (
          <PictureChip key={p.id} kind={kind} ownerId={ownerId} picture={p} onOpen={() => setLightboxId(p.id)} />
        ))}
        <button
          type="button"
          className={live ? 'btn btn-small' : 'btn btn-small demo-disabled'}
          disabled={!live || upload.isPending}
          title={live ? undefined : DEMO_HINT}
          onClick={() => fileRef.current?.click()}
        >
          {upload.isPending ? 'Uploading…' : '+ Add photo'}
        </button>
        {coarsePointer && (
          <button
            type="button"
            className={live ? 'btn btn-small' : 'btn btn-small demo-disabled'}
            disabled={!live || upload.isPending}
            title={live ? undefined : DEMO_HINT}
            onClick={() => cameraRef.current?.click()}
          >
            Take photo
          </button>
        )}
      </div>
      {error && <div className="pic-error">{error}</div>}
      <input ref={fileRef} type="file" accept="image/*" hidden onChange={onPick} />
      <input ref={cameraRef} type="file" accept="image/*" capture="environment" hidden onChange={onPick} />
      {lightboxId != null && (
        <PictureLightbox kind={kind} ownerId={ownerId} pictureId={lightboxId} live={live} onClose={() => setLightboxId(null)} />
      )}
    </div>
  );
}
