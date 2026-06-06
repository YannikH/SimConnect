import { useEffect, useRef, useState } from "react";
import { Flex } from "./Structure";
import { LGraph, LGraphCanvas, LiteGraph } from "litegraph.js";

import "litegraph.js/css/litegraph.css";
import { OutputNode } from "../Data/NodeLoader";
import { PostMessage } from "../Data/WebviewHandler";

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
    const outAddresses = this.GetAllOutputs().map((o) => o.biosOutput.address);
    PostMessage({ type: "OutputsChanged", data: outAddresses });
    localStorage.setItem(CACHE_NAME, JSON.stringify(this.serialize()));
  }
}

class LitegraphManager {
  readonly graph = new LGraphExtended();
  private canvas?: LGraphCanvas;

  constructor() {
    const cache = localStorage.getItem(CACHE_NAME);
    if (cache) {
      this.graph.load(cache);
    }
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
    <Flex $fullHeight $fullWidth>
      <canvas ref={canvasRef} width="1024" height="720" />
    </Flex>
  );
};

export default GraphEditor;
