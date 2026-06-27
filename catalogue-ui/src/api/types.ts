// TypeScript mirrors of the API's DTOs. These are *compile-time only* — they
// describe the JSON shape so the editor can autocomplete and catch mistakes.
// There's no runtime check that the server actually sends this shape (unlike C#
// deserialization, which would throw). We trust the contract; types just help us.

// C#: public sealed record RoomResponse(int Id, string Name, string? Description,
//                                       int LocationId, byte[] RowVersion);
export interface RoomResponse {
  id: number;                 // C# int      -> number
  name: string;               // C# string   -> string
  description: string | null; // C# string?  -> string | null
  locationId: number;
  rowVersion: string;         // C# byte[]   -> base64 string in JSON
}

// C#: public sealed record LocationResponse(int Id, string Name, string? Description,
//                                           IReadOnlyList<RoomResponse> Rooms, byte[] RowVersion);
export interface LocationResponse {
  id: number;
  name: string;
  description: string | null;
  rooms: RoomResponse[];      // C# IReadOnlyList<T> -> T[]
  rowVersion: string;
}

// C#: public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int TotalCount, ...);
// Generics work just like C#: PagedResponse<LocationResponse>.
export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNext: boolean;
  hasPrevious: boolean;
}
