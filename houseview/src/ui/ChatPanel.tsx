import { useEffect, useRef, useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { ApiError, DEMO_HINT } from '../api';
import { parseChatSegments, sendChat, type ChatTurn, type HabitatLink } from '../chat';

interface ChatPanelProps {
  /** Demo mode shows the toggle greyed-out so the feature is discoverable but inert. */
  live: boolean;
  /** Fly the camera / open the detail panel for an entity the assistant linked. */
  onOpenLink: (link: HabitatLink) => void;
}

/**
 * The AI assistant: a collapsible glass panel docked bottom-left, mirroring the DetailPanel's
 * look on the opposite side. Conversation state lives here (and dies with a refresh — the
 * backend is stateless by design); mutations the assistant performs are picked up by
 * invalidating the catalogue query, so the 3D scene refreshes on its own.
 */
export function ChatPanel({ live, onOpenLink }: ChatPanelProps) {
  const [open, setOpen] = useState(false);
  const [messages, setMessages] = useState<ChatTurn[]>([]);
  const [draft, setDraft] = useState('');
  const logRef = useRef<HTMLDivElement>(null);
  const queryClient = useQueryClient();

  const chat = useMutation({
    mutationFn: sendChat,
    onSuccess: (result) => {
      setMessages((prev) => [...prev, { role: 'assistant', content: result.reply }]);
      // The assistant may have created/moved/deleted things; refresh the scene.
      queryClient.invalidateQueries({ queryKey: ['catalogue'] });
    },
  });

  // Keep the newest message in view as the conversation grows.
  useEffect(() => {
    logRef.current?.scrollTo({ top: logRef.current.scrollHeight });
  }, [messages, chat.isPending, open]);

  const send = () => {
    const content = draft.trim();
    if (!content || chat.isPending) return;
    const next: ChatTurn[] = [...messages, { role: 'user', content }];
    setMessages(next);
    setDraft('');
    chat.mutate(next);
  };

  // Dev ProblemDetails carry a multi-line stack trace in detail; only the first line is useful
  // to a person (and production scrubs detail entirely, falling back to the title).
  const errorText =
    chat.error instanceof ApiError
      ? (chat.error.problem?.detail ?? chat.error.message).split(/\r?\n/)[0].replace(/^System\.\w+Exception: /, '')
      : chat.error
        ? 'The assistant is unreachable right now.'
        : null;

  if (!open || !live) {
    return (
      <button
        className={live ? 'chat-toggle' : 'chat-toggle demo-disabled'}
        onClick={() => setOpen(true)}
        disabled={!live}
        title={live ? undefined : DEMO_HINT}
        aria-label="Open assistant"
      >
        <span className="chat-toggle-star" aria-hidden="true">
          ✳
        </span>
        Ask Habitat
      </button>
    );
  }

  return (
    <aside className="chat-panel" aria-label="Habitat assistant">
      <header className="chat-header">
        <span className="panel-kicker">Assistant</span>
        <button className="panel-close" onClick={() => setOpen(false)} aria-label="Close assistant">
          ×
        </button>
      </header>

      <div className="chat-log" ref={logRef}>
        {messages.length === 0 && (
          <p className="chat-hint">
            Ask about your inventory — “where's the drill?” — or make changes: “add a hammer to the
            garage toolbox”.
          </p>
        )}
        {messages.map((turn, i) =>
          turn.role === 'user' ? (
            <div key={i} className="chat-msg chat-msg-user">
              {turn.content}
            </div>
          ) : (
            <div key={i} className="chat-msg chat-msg-assistant">
              {parseChatSegments(turn.content).map((segment, j) => {
                switch (segment.type) {
                  case 'habitat':
                    return (
                      <button key={j} className="chat-link" onClick={() => onOpenLink(segment.link)}>
                        {segment.label}
                      </button>
                    );
                  case 'url':
                    return (
                      <a key={j} href={segment.href} target="_blank" rel="noreferrer">
                        {segment.label}
                      </a>
                    );
                  default:
                    return <span key={j}>{segment.text}</span>;
                }
              })}
            </div>
          ),
        )}
        {chat.isPending && (
          <div className="chat-msg chat-msg-assistant chat-typing" aria-label="Assistant is thinking">
            <span />
            <span />
            <span />
          </div>
        )}
        {errorText && !chat.isPending && <div className="chat-error">{errorText}</div>}
      </div>

      <div className="chat-input-row">
        <textarea
          className="chat-input"
          rows={1}
          placeholder="Ask or instruct…"
          value={draft}
          disabled={chat.isPending}
          onChange={(e) => setDraft(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
              e.preventDefault();
              send();
            }
          }}
        />
        <button className="chat-send" onClick={send} disabled={chat.isPending || !draft.trim()}>
          Send
        </button>
      </div>
    </aside>
  );
}
