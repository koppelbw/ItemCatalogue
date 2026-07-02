import { useState } from 'react';
import { mapApiError, useAddCollectionItem, useCollections, useRemoveCollectionItem } from '../mutations';
import type { CollectionResponse, ItemResponse } from '../types';

interface Props {
  collection: CollectionResponse;
  items: ItemResponse[];
  onClose: () => void;
}

// Manage a collection's rich-join membership (item + quantity + role). Reads the
// live collection from the query so it reflects adds/removes immediately.
export function CollectionMembers({ collection, items, onClose }: Props) {
  const collectionsQuery = useCollections();
  const fresh = collectionsQuery.data?.find((c) => c.id === collection.id) ?? collection;

  const add = useAddCollectionItem();
  const remove = useRemoveCollectionItem();

  const [itemId, setItemId] = useState<number>(0);
  const [quantity, setQuantity] = useState<number>(1);
  const [role, setRole] = useState<string>('');
  const [banner, setBanner] = useState<string | null>(null);

  const memberIds = new Set(fresh.items.map((m) => m.itemId));
  const addable = items.filter((i) => !memberIds.has(i.id) && !i.isDeleted);

  const onAdd = async () => {
    setBanner(null);
    if (itemId <= 0) {
      setBanner('Pick an item to add.');
      return;
    }
    try {
      await add.mutateAsync({ collectionId: collection.id, itemId, quantity, sortOrder: null, role: role || null });
      setItemId(0);
      setQuantity(1);
      setRole('');
    } catch (e) {
      setBanner(mapApiError(e).banner);
    }
  };

  const onRemove = async (memberItemId: number) => {
    setBanner(null);
    try {
      await remove.mutateAsync({ collectionId: collection.id, itemId: memberItemId });
    } catch (e) {
      setBanner(mapApiError(e).banner);
    }
  };

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal" role="dialog" aria-modal="true" onClick={(e) => e.stopPropagation()}>
        <header className="modal-header">
          <h3>“{fresh.name}” members</h3>
          <button className="modal-close" onClick={onClose} aria-label="Close">×</button>
        </header>
        {banner && <div className="form-banner">{banner}</div>}
        <div className="modal-body">
          {fresh.items.length === 0 ? (
            <p>No items in this collection yet.</p>
          ) : (
            <table className="manage-table">
              <thead><tr><th>Item</th><th>Qty</th><th>Role</th><th></th></tr></thead>
              <tbody>
                {fresh.items.map((m) => (
                  <tr key={m.itemId}>
                    <td>{m.itemName}</td>
                    <td>{m.quantity}</td>
                    <td>{m.role ?? '—'}</td>
                    <td className="row-actions">
                      <button className="btn btn-small btn-danger" onClick={() => onRemove(m.itemId)} disabled={remove.isPending}>
                        Remove
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}

          <div className="manage-add-row">
            <select value={itemId || ''} onChange={(e) => setItemId(Number(e.target.value))}>
              <option value="">Add an item…</option>
              {addable.map((i) => (
                <option key={i.id} value={i.id}>
                  {i.name}
                </option>
              ))}
            </select>
            <input
              type="number"
              min={1}
              value={quantity}
              onChange={(e) => setQuantity(Math.max(1, parseInt(e.target.value, 10) || 1))}
              aria-label="Quantity"
              style={{ width: '4.5rem' }}
            />
            <input type="text" placeholder="Role (optional)" value={role} onChange={(e) => setRole(e.target.value)} />
            <button className="btn btn-primary btn-small" onClick={onAdd} disabled={add.isPending}>
              Add
            </button>
          </div>
        </div>
        <footer className="modal-footer">
          <button className="btn btn-ghost" onClick={onClose}>Done</button>
        </footer>
      </div>
    </div>
  );
}
