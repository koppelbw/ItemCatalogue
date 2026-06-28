import { useEffect, type ReactNode } from 'react';

interface ModalProps {
  title: string;
  onClose: () => void;
  children: ReactNode;
  /** rendered in the footer, typically Save / Cancel */
  footer: ReactNode;
  /** a server/banner error shown above the body */
  banner?: string | null;
}

export function Modal({ title, onClose, children, footer, banner }: ModalProps) {
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [onClose]);

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal" role="dialog" aria-modal="true" aria-label={title} onClick={(e) => e.stopPropagation()}>
        <header className="modal-header">
          <h3>{title}</h3>
          <button className="modal-close" onClick={onClose} aria-label="Close">
            ×
          </button>
        </header>
        {banner && <div className="form-banner">{banner}</div>}
        <div className="modal-body">{children}</div>
        <footer className="modal-footer">{footer}</footer>
      </div>
    </div>
  );
}
