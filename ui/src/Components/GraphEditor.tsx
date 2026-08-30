import { useEffect, useRef, useState } from "react";
import { Flex } from "./Structure";
import { LGraph, LGraphCanvas, LiteGraph } from "litegraph.js";

import "litegraph.js/css/litegraph.css";
import { OutputNode } from "../Data/NodeLoader";
import { PostMessage } from "../Data/WebviewHandler";
import { Button, Typography } from "@mui/material";

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

class LitegraphManager {
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
      // console.log(helper, value);
      const types = Object.keys(LiteGraph.registered_node_types).filter((t) =>
        t.toLowerCase().includes(value.toLowerCase())
      );
      // console.log(types);
      return types;
      // return [];
    };
    this.graph.start(10);
    this.graph.broadcastOutputs();
  }
  stop() {}
}

const GraphEditor = () => {
  const [lgManager] = useState<LitegraphManager>(() => new LitegraphManager());
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const [graphNames, setGraphNames] = useState<string[]>([]);
  const [loadedName, setLoadedName] = useState<string>("");

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    lgManager.setGraphListListener(setGraphNames);
    lgManager.setGraphLoadedListener(setLoadedName);
    lgManager.startCanvas(canvas);
    return () => lgManager.stop();
  }, [lgManager]);

  return (
    <Flex $fullHeight $grow $hideOverflow $row>
      <Flex $column $fullHeight style={{ width: 200, borderRight: "1px solid #444" }}>
        <Typography variant="h6">Graphs</Typography>
        <Flex $column $grow style={{overflowY: "auto"}}>
          {graphNames.length === 0 && (
            <div style={{ padding: "8px", opacity: 0.6 }}>No saved graphs</div>
          )}
          {graphNames.map((name) => (
            <div
              key={name}
              onClick={() => lgManager.loadGraph(name)}
              title={name}
              style={{
                padding: "6px 8px",
                cursor: "pointer",
                whiteSpace: "nowrap",
                overflow: "hidden",
                textOverflow: "ellipsis",
                background: name === loadedName ? "rgba(128,128,128,0.3)" : "transparent",
              }}
            >
              {name}
            </div>
          ))}
        </Flex>
      </Flex>
      <Flex $column $grow $hideOverflow $fullHeight>
        <Flex $row $fullWidth style={{ height: "30px", gap: "8px", alignItems: "center", padding: "0 8px" }}>
          <Button onClick={() => lgManager.loadGraphDialog()} variant="contained">
            Load...
          </Button>
          <Button onClick={() => lgManager.saveGraphDialog()} variant="contained">
            Save...
          </Button>
          {loadedName && <span>{loadedName}</span>}
        </Flex>
        <Flex $grow $hideOverflow $fullWidth>
          <Flex $fullWidth $fullHeight>
            <canvas ref={canvasRef} width="1024" height="720" />
          </Flex>
        </Flex>
      </Flex>
    </Flex>
  );
};

export default GraphEditor;
