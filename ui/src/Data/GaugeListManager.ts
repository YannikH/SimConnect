import { GaugeListSchemaV1, type GaugeListV1 } from "./GaugeData";

// Holds the latest gauge list regardless of which page is active, so switching away from the
// Graphs page and back doesn't lose it - window.trc.setGauges is only called once by the host,
// not every time GaugeList remounts.
class GaugeListManager {
  private gauges: GaugeListV1 = [];
  private listener?: (gauges: GaugeListV1) => void;

  attach() {
    window.trc = {
      ...window.trc,
      setGauges: (data: unknown) => this.onGauges(data),
    };
  }

  private onGauges(data: unknown) {
    const result = GaugeListSchemaV1.safeParse(data);
    if (!result.success) {
      console.error("Failed to parse gauge list", data, result.error);
      return;
    }
    this.gauges = result.data;
    this.listener?.(this.gauges);
  }

  setListener(listener: ((gauges: GaugeListV1) => void) | undefined) {
    this.listener = listener;
    listener?.(this.gauges);
  }
}

export const gaugeListManager = new GaugeListManager();
