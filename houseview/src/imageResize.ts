// Client-side downscale for picture uploads. Phone camera shots routinely run
// 4–12 MB; the API caps uploads at 10 MB and blob storage/egress ride on a
// limited Azure credit, so anything big is resized to a web-friendly JPEG
// before it leaves the browser.

const MAX_EDGE_PX = 2048;
const SKIP_BELOW_BYTES = 1024 * 1024;
const JPEG_QUALITY = 0.85;

/** Mirrors PictureValidationRules.AllowedContentTypes on the API. */
export const ALLOWED_PICTURE_TYPES = ['image/jpeg', 'image/png', 'image/webp', 'image/gif'] as const;

export function isAllowedPictureType(type: string): boolean {
  return (ALLOWED_PICTURE_TYPES as readonly string[]).includes(type);
}

/**
 * Returns the file to actually upload: the original when it is already small
 * (≤1 MB and ≤2048 px on its longest edge), a GIF (re-encoding would drop the
 * animation), or undecodable (let the server's magic-byte sniff reject it);
 * otherwise a canvas-downscaled JPEG. EXIF orientation is baked in by
 * createImageBitmap so portrait phone photos stay upright.
 */
export async function prepareImage(file: File): Promise<File> {
  if (file.type === 'image/gif') {
    return file;
  }

  let bitmap: ImageBitmap;
  try {
    bitmap = await createImageBitmap(file, { imageOrientation: 'from-image' });
  } catch {
    return file;
  }

  const scale = Math.min(1, MAX_EDGE_PX / Math.max(bitmap.width, bitmap.height));
  if (scale === 1 && file.size <= SKIP_BELOW_BYTES) {
    bitmap.close();
    return file;
  }

  const canvas = document.createElement('canvas');
  canvas.width = Math.max(1, Math.round(bitmap.width * scale));
  canvas.height = Math.max(1, Math.round(bitmap.height * scale));
  const ctx = canvas.getContext('2d');
  if (!ctx) {
    bitmap.close();
    return file;
  }

  // JPEG has no alpha channel; composite transparent PNGs onto white instead of
  // letting the encoder fill with black.
  ctx.fillStyle = '#ffffff';
  ctx.fillRect(0, 0, canvas.width, canvas.height);
  ctx.drawImage(bitmap, 0, 0, canvas.width, canvas.height);
  bitmap.close();

  const blob = await new Promise<Blob | null>((resolve) => canvas.toBlob(resolve, 'image/jpeg', JPEG_QUALITY));
  if (!blob) {
    return file;
  }

  const baseName = file.name.replace(/\.[^.]+$/, '') || 'photo';
  return new File([blob], `${baseName}.jpg`, { type: 'image/jpeg' });
}
