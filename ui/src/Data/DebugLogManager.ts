import * as z from "zod";

export const GaugeUpdateEntrySchema = z.object({
  time: z.string(),
  gaugeType: z.string(),
  gaugeId: z.number(),
  data: z.unknown(),
});
export type GaugeUpdateEntry = z.infer<typeof GaugeUpdateEntrySchema>;

const MAX_ENTRIES = 300;

// Buffers gauge-update entries regardless of which page is active, so switching away from the
// Debug page and back doesn't lose history - unlike GaugeList, whose window.trc handler only
// exists while that page is mounted.
class DebugLogManager {
  private entries: GaugeUpdateEntry[] = [];
  private listener?: (entries: GaugeUpdateEntry[]) => void;

  attach() {
    window.trc = {
      ...window.trc,
      onGaugeUpdate: (data: unknown) => this.onGaugeUpdate(data),
    };
  }

  private onGaugeUpdate(data: unknown) {
    const result = GaugeUpdateEntrySchema.safeParse(data);
    if (!result.success) {
      console.error("Failed to parse gauge update entry", data, result.error);
      return;
    }
    this.entries = [...this.entries, result.data].slice(-MAX_ENTRIES);
    this.listener?.(this.entries);
  }

  setListener(listener: ((entries: GaugeUpdateEntry[]) => void) | undefined) {
    this.listener = listener;
    listener?.(this.entries);
  }
}

export const debugLogManager = new DebugLogManager();
