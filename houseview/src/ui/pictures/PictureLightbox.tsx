import { useState } from 'react';
import { mapApiError, useDeletePicture, usePictures, useUpdatePicture } from '../../mutations';
import type { PictureOwnerKind } from '../../types';
import { Modal } from '../forms/Modal';

interface PictureLightboxProps {
  kind: PictureOwnerKind;
  ownerId: number;
  pictureId: number;
  live: boolean;
  onClose: () => void;
}

/**
 * Full-size view of one picture with its metadata actions: inline caption
 * editing, promoting it to the owner's cover photo, and deletion. The picture
 * is looked up from the live query by id (not carried as a prop) so a
 * successful update re-renders with the fresh rowVersion instead of going
 * stale and 409ing on the next edit.
 */
export function PictureLightbox({ kind, ownerId, pictureId, live, onClose }: PictureLightboxProps) {
  const { data: pictures } = usePictures(kind, ownerId, live);
  const update = useUpdatePicture();
  const remove = useDeletePicture();
  const [caption, setCaption] = useState<string | null>(null); // null = not edited yet
  const [banner, setBanner] = useState<string | null>(null);
  const [confirmingDelete, setConfirmingDelete] = useState(false);

  const picture = pictures?.find((p) => p.id === pictureId);
  if (!picture) {
    // deleted (possibly by this dialog) or the list refetch dropped it
    return null;
  }

  const busy = update.isPending || remove.isPending;
  const editedCaption = caption ?? picture.caption ?? '';
  const captionDirty = editedCaption !== (picture.caption ?? '');

  const save = (patch: Partial<{ caption: string | null; isPrimary: boolean }>) => {
    setBanner(null);
    update.mutate(
      {
        kind,
        ownerId,
        body: {
          id: picture.id,
          caption: patch.caption !== undefined ? patch.caption : picture.caption,
          isPrimary: patch.isPrimary !== undefined ? patch.isPrimary : picture.isPrimary,
          sortOrder: picture.sortOrder,
          rowVersion: picture.rowVersion,
        },
      },
      { onError: (err) => setBanner(mapApiError(err).banner) },
    );
  };

  const doDelete = () => {
    setBanner(null);
    remove.mutate(
      { kind, ownerId, id: picture.id },
      { onSuccess: onClose, onError: (err) => setBanner(mapApiError(err).banner) },
    );
  };

  const meta = [
    picture.originalFileName,
    `${Math.max(1, Math.round(picture.sizeBytes / 1024))} KB`,
    new Date(picture.createdDate).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' }),
  ]
    .filter(Boolean)
    .join(' · ');

  return (
    <Modal
      title={picture.caption ?? 'Photo'}
      onClose={onClose}
      banner={banner}
      footer={
        <>
          {confirmingDelete ? (
            <>
              <span className="pic-confirm-label">Delete this photo?</span>
              <button className="btn btn-small btn-danger" onClick={doDelete} disabled={busy}>
                Yes, delete
              </button>
              <button className="btn btn-small" onClick={() => setConfirmingDelete(false)} disabled={busy}>
                Keep it
              </button>
            </>
          ) : (
            <>
              <button className="btn btn-small btn-danger" onClick={() => setConfirmingDelete(true)} disabled={busy}>
                Delete
              </button>
              {!picture.isPrimary && (
                <button className="btn btn-small" onClick={() => save({ isPrimary: true })} disabled={busy}>
                  Set as cover
                </button>
              )}
              {captionDirty && (
                <button className="btn btn-small btn-primary" onClick={() => save({ caption: editedCaption || null })} disabled={busy}>
                  Save caption
                </button>
              )}
              <button className="btn btn-small" onClick={onClose}>
                Close
              </button>
            </>
          )}
        </>
      }
    >
      <div className="pic-lightbox">
        <img src={picture.url} alt={picture.caption ?? 'Photo'} />
        <input
          className="pic-caption-input"
          type="text"
          placeholder="Add a caption…"
          maxLength={500}
          value={editedCaption}
          onChange={(e) => setCaption(e.target.value)}
        />
        <div className="pic-lightbox-meta">
          {picture.isPrimary && <span className="pic-cover-badge">Cover photo</span>}
          {meta}
        </div>
      </div>
    </Modal>
  );
}
