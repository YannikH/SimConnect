import { useEffect, useRef, useState } from "react";
import { Flex } from "./Structure";
import { LGraph, LGraphCanvas, LiteGraph } from "litegraph.js";

import "litegraph.js/css/litegraph.css";
import { OutputNode } from "../Data/NodeLoader";
import { PostMessage } from "../Data/WebviewHandler";
import { Button } from "@mui/material";

const CACHE_NAME = "BIOS_GRAPH_CACHE";

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
    console.log("DATA RECEIVED", address, value);
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
    PostMessage({ type: "OutputsChanged", data: outAddresses });
  }
}

class LitegraphManager {
  readonly graph = new LGraphExtended();
  private canvas?: LGraphCanvas;

  loadCache() {
    const cache = localStorage.getItem(CACHE_NAME);
    if (cache) {
      console.log("LOADING", cache);
      this.graph.configure(JSON.parse(cache));
    }
    this.graph.broadcastOutputs();
  }

  saveCache() {
    localStorage.setItem(CACHE_NAME, JSON.stringify(this.graph.serialize()));
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
  }
  stop() {}
}

const GraphEditor = () => {
  const [lgManager] = useState<LitegraphManager>(() => new LitegraphManager());
  const canvasRef = useRef<HTMLCanvasElement | null>(null);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    lgManager.startCanvas(canvas);
    return () => lgManager.stop();
  }, [lgManager]);

  return (
    <Flex $fullHeight $fullWidth $column>
      <Flex $row $fullWidth style={{height: "30px"}}>
        <Button onClick={() => lgManager.loadCache()} variant="contained">Load</Button>
        <Button onClick={() => lgManager.saveCache()} variant="contained">Save</Button>
      </Flex>
      <Flex $grow $hideOverflow>
        <canvas ref={canvasRef} width="1024" height="720" />
      </Flex>
    </Flex>
  );
};

export default GraphEditor;
