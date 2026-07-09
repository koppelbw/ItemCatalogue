import { apiPost } from './api';

// Client side of POST /api/chat. The backend is stateless: we send the whole visible
// conversation every time and get the assistant's next reply back. Unlike loadCatalogue there is
// deliberately no timeout — a turn that chains several tool calls can take tens of seconds.

export interface ChatTurn {
  role: 'user' | 'assistant';
  content: string;
}

export interface ChatReply {
  reply: string;
  toolCallCount: number;
  inputTokens: number;
  outputTokens: number;
}

/** The backend caps a conversation at 40 turns; trim locally so long chats keep working. */
export const MAX_SENT_TURNS = 24;

export function sendChat(messages: ChatTurn[]): Promise<ChatReply> {
  return apiPost<ChatReply>('chat', { messages: messages.slice(-MAX_SENT_TURNS) });
}

// ---------------------------------------------------------------------------
// habitat:// deep links
// ---------------------------------------------------------------------------

export type HabitatLinkKind = 'item' | 'room' | 'container' | 'location';

export interface HabitatLink {
  kind: HabitatLinkKind;
  id: number;
}

/** A piece of an assistant reply: plain text, a clickable habitat link, or an external URL. */
export type ChatSegment =
  | { type: 'text'; text: string }
  | { type: 'habitat'; label: string; link: HabitatLink }
  | { type: 'url'; label: string; href: string };

const LINK_PATTERN = /\[([^\]]+)\]\((habitat:\/\/(item|room|container|location)\/(\d+)|https?:\/\/[^\s)]+)\)/g;

/**
 * Splits a markdown-ish assistant reply into renderable segments. Only link syntax is parsed —
 * the system prompt keeps replies short and plain, so full markdown rendering isn't warranted.
 */
export function parseChatSegments(reply: string): ChatSegment[] {
  const segments: ChatSegment[] = [];
  let last = 0;

  for (const match of reply.matchAll(LINK_PATTERN)) {
    if (match.index > last) {
      segments.push({ type: 'text', text: reply.slice(last, match.index) });
    }
    const [, label, target, kind, id] = match;
    if (kind && id) {
      segments.push({ type: 'habitat', label, link: { kind: kind as HabitatLinkKind, id: Number(id) } });
    } else {
      segments.push({ type: 'url', label, href: target });
    }
    last = match.index + match[0].length;
  }

  if (last < reply.length) {
    segments.push({ type: 'text', text: reply.slice(last) });
  }

  return segments;
}
