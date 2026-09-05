export const PAGES = [
  { id: "graphs", label: "Graphs" },
  { id: "gamepads", label: "Gamepads" },
  { id: "debug", label: "Debug" },
] as const;

export type PageId = (typeof PAGES)[number]["id"];
