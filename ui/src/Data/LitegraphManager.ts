import { LGraph, LGraphCanvas, LGraphNode, LiteGraph } from "litegraph.js";
import { OutputNode } from "./NodeLoader";
import { PostMessage } from "./WebviewHandler";

class LGraphExtended extends LGraph {
  // Set by LitegraphManager - notified on every graph change (widget edits, rewiring,
  // node moves...), regardless of cause.
  onGraphChanged?: () => void;

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

  // Live BIOS telemetry display only - not graph execution. Independent of whether a graph
  // is currently activated on the C# side.
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
  }

  afterChange(): void {
    this.broadcastOutputs();
    this.onGraphChanged?.();
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
  private onGraphDeactivatedListener?: () => void;
  private isActivated = false;
  private reactivateTimer?: ReturnType<typeof setTimeout>;

  constructor() {
    window.dcs = {
      ...window.dcs,
      setGraphList: (names: string[]) => this.onGraphListChanged?.(names),
      onGraphLoaded: (name: string, data: unknown) => {
        this.applyLoadedGraph(data);
        this.onGraphLoaded?.(name);
      },
      setNodeValues: (values: Array<[number, number, number]>) => this.applyNodeValues(values),
      onGraphDeactivated: () => {
        this.isActivated = false;
        clearTimeout(this.reactivateTimer);
        this.onGraphDeactivatedListener?.();
      },
    };
    this.graph.onGraphChanged = () => this.scheduleReactivate();
  }

  // Any edit while a graph is already running on the device re-sends it, so e.g. tweaking a
  // Number node's value shows up immediately - debounced so dragging/typing doesn't flood the
  // WebView bridge (and C#'s full recompile-and-persist-to-disk) on every intermediate change.
  private scheduleReactivate() {
    if (!this.isActivated) return;
    clearTimeout(this.reactivateTimer);
    this.reactivateTimer = setTimeout(() => this.runOnDevice(), 200);
  }

  setGraphListListener(listener: (names: string[]) => void) {
    this.onGraphListChanged = listener;
  }

  setGraphLoadedListener(listener: (name: string) => void) {
    this.onGraphLoaded = listener;
  }

  setGraphDeactivatedListener(listener: () => void) {
    this.onGraphDeactivatedListener = listener;
  }

  applyLoadedGraph(data: unknown) {
    this.graph.configure(data as never);
    this.graph.broadcastOutputs();
  }

  // Live values pushed back from the C#-side execution engine - display only, never recomputed
  // or re-sent anywhere by the browser.
  applyNodeValues(values: Array<[number, number, number]>) {
    const nodesById = new Map<number, LGraphNode>();
    for (const node of this.graph["_nodes"] as LGraphNode[]) {
      nodesById.set(node.id, node);
    }
    for (const [nodeId, slot, value] of values) {
      nodesById.get(nodeId)?.setOutputData(slot, value);
    }
    this.graph.setDirtyCanvas(true, true);
  }

  // Loads a known graph from the Documents/SimConnect folder by name (sidebar click).
  loadGraph(name: string) {
    PostMessage({ type: "LoadGraph", data: { name } });
  }

  // Re-requests the graph list from disk (sidebar refresh button).
  refreshGraphList() {
    PostMessage({ type: "RequestGraphList" });
  }

  // Saves back to wherever this graph was last loaded from/saved to. C# falls back to the
  // "Save As" dialog if nothing's been loaded/saved yet (a brand-new graph).
  saveGraph() {
    PostMessage({ type: "SaveGraph", data: { graph: this.graph.serialize() } });
  }

  // Opens a native "Save As" dialog so the user can save anywhere on disk.
  saveGraphDialog() {
    PostMessage({ type: "SaveGraphDialog", data: { graph: this.graph.serialize() } });
  }

  // Opens a native "Open" dialog so the user can load from anywhere on disk.
  loadGraphDialog() {
    PostMessage({ type: "LoadGraphDialog" });
  }

  // Hands the current graph to C# to actually run (drives hardware, even while minimized).
  runOnDevice() {
    this.isActivated = true;
    PostMessage({ type: "ActivateGraph", data: { graph: this.graph.serialize() } });
  }

  stopOnDevice() {
    this.isActivated = false;
    clearTimeout(this.reactivateTimer);
    PostMessage({ type: "DeactivateGraph" });
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
    this.graph.broadcastOutputs();
  }
  stop() {}
}

// Single shared instance: the sidebar (graph list) and the canvas editor both
// need to act on the same LGraph, even though they're no longer in the same component.
export const litegraphManager = new LitegraphManager();
