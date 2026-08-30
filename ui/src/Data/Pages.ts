export const PAGES = [
  { id: "graphs", label: "Graphs" },
  { id: "gamepads", label: "Gamepads" },
] as const;

export type PageId = (typeof PAGES)[number]["id"];
