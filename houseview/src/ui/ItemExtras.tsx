import { mapApiError, useItemEvents, useItemTags, useSetItemTags, useTags } from '../mutations';
import { ITEM_EVENT_TYPE_LABELS } from '../types';

// Tag assignment + event history for an existing item, shown inside the item
// edit form. Both are item-scoped relationships the main item write doesn't carry.

export function ItemExtras({ itemId }: { itemId: number }) {
  return (
    <div className="item-extras">
      <TagEditor itemId={itemId} />
      <EventTimeline itemId={itemId} />
    </div>
  );
}

function TagEditor({ itemId }: { itemId: number }) {
  const itemTags = useItemTags(itemId);
  const allTags = useTags();
  const setTags = useSetItemTags();

  const current = new Set((itemTags.data?.tags ?? []).map((t) => t.id));

  const toggle = async (tagId: number) => {
    const next = new Set(current);
    if (next.has(tagId)) next.delete(tagId);
    else next.add(tagId);
    try {
      await setTags.mutateAsync({ itemId, tagIds: [...next] });
    } catch {
      /* surfaced inline below via the query refetch; ignore here */
    }
  };

  const tags = allTags.data ?? [];

  return (
    <div className="form-field">
      <span>Tags</span>
      {tags.length === 0 ? (
        <p className="form-hint">No tags defined yet — create some on the Tags tab.</p>
      ) : (
        <div className="form-chips">
          {tags.map((t) => (
            <button
              type="button"
              key={t.id}
              className={`form-chip${current.has(t.id) ? ' on' : ''}`}
              onClick={() => toggle(t.id)}
              disabled={setTags.isPending}
            >
              {t.name}
            </button>
          ))}
        </div>
      )}
      {setTags.isError && <em className="form-error">{mapApiError(setTags.error).banner}</em>}
    </div>
  );
}

function EventTimeline({ itemId }: { itemId: number }) {
  const events = useItemEvents(itemId);
  const list = events.data ?? [];

  return (
    <div className="form-field">
      <span>History</span>
      {list.length === 0 ? (
        <p className="form-hint">No recorded events.</p>
      ) : (
        <ul className="event-list">
          {list.map((e) => (
            <li key={e.id}>
              <strong>{ITEM_EVENT_TYPE_LABELS[e.eventType] ?? e.eventType}</strong>
              <time>{new Date(e.occurredAt).toLocaleString()}</time>
              {(e.oldValue || e.newValue) && (
                <span className="event-change">
                  {e.oldValue ?? '—'} → {e.newValue ?? '—'}
                </span>
              )}
              {e.notes && <span className="event-notes">{e.notes}</span>}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
