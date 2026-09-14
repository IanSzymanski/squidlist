export interface Playlist { id: string; title: string; description?: string; }
/** Entry identity permits repeated media within the same playlist. */
export interface PlaylistEntry {
  id: string;
  playlistId: string;
  mediaId: string;
  position: number;
}
