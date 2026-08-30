import { LGraph, LGraphCanvas, LiteGraph } from "litegraph.js";
import { OutputNode } from "./NodeLoader";
import { PostMessage } from "./WebviewHandler";

class LGraphExtended extends LGraph {
  constructor() {
    super();
    window.dcs = {
      ...window.dcs,
      setData: this.onData.bind(this),
    };
  }

  GetAllOutputs(): OutputNode[] {
    const allNodes = this["_nodes"];
    return allNodes.filter((n) => n instanceof OutputNode);
  }

  onData(address: number, value: number) {
    for (const node of this.GetAllOutputs()) {
      const output = node.biosOutput;
      if (output.address === address) {
        node.valueRaw = value & output.mask;
        node.value = (value & output.mask) / output.max_value;
        node.updateOutputs();
      }
    }
    this.setDirtyCanvas(true, true);
    this.start();
    this.stop();
  }

  afterChange(): void {
    this.broadcastOutputs();
  }

  broadcastOutputs(): void {
    const outAddresses = this.GetAllOutputs().map((o) => o.biosOutput.address);
    console.log('outputs changed', outAddresses);
    PostMessage({ type: "OutputsChanged", data: outAddresses });
  }
}

export class LitegraphManager {
  readonly graph = new LGraphExtended();
  private canvas?: LGraphCanvas;
  private onGraphListChanged?: (names: string[]) => void;
  private onGraphLoaded?: (name: string) => void;

  constructor() {
    window.dcs = {
      ...window.dcs,
      setGraphList: (names: string[]) => this.onGraphListChanged?.(names),
      onGraphLoaded: (name: string, data: unknown) => {
        this.applyLoadedGraph(data);
        this.onGraphLoaded?.(name);
      },
    };
  }

  setGraphListListener(listener: (names: string[]) => void) {
    this.onGraphListChanged = listener;
  }

  setGraphLoadedListener(listener: (name: string) => void) {
    this.onGraphLoaded = listener;
  }

  applyLoadedGraph(data: unknown) {
    this.graph.configure(data as never);
    this.graph.start(10);
    this.graph.broadcastOutputs();
  }

  // Loads a known graph from the Documents/SimConnect folder by name (sidebar click).
  loadGraph(name: string) {
    PostMessage({ type: "LoadGraph", data: { name } });
  }

  // Re-requests the graph list from disk (sidebar refresh button).
  refreshGraphList() {
    PostMessage({ type: "RequestGraphList" });
  }

  // Opens a native "Save As" dialog so the user can save anywhere on disk.
  saveGraphDialog() {
    PostMessage({ type: "SaveGraphDialog", data: { graph: this.graph.serialize() } });
  }

  // Opens a native "Open" dialog so the user can load from anywhere on disk.
  loadGraphDialog() {
    PostMessage({ type: "LoadGraphDialog" });
  }

  startCanvas(canvasEl: HTMLCanvasElement) {
    this.canvas = new LGraphCanvas(canvasEl, this.graph);
    this.canvas.autoresize = true;

    LiteGraph.searchbox_extras = {};
    this.canvas.onSearchBox = (_: Element, value: string) => {
      const types = Object.keys(LiteGraph.registered_node_types).filter((t) =>
        t.toLowerCase().includes(value.toLowerCase())
      );
      return types;
    };
    this.graph.start(10);
    this.graph.broadcastOutputs();
  }
  stop() {}
}

// Single shared instance: the sidebar (graph list) and the canvas editor both
// need to act on the same LGraph, even though they're no longer in the same component.
export const litegraphManager = new LitegraphManager();
