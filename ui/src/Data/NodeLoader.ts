
import { LiteGraph } from "litegraph.js";
import { type BiosAircraftV1, type BiosCategoryV1, type BiosControlV1, type IntegerOutputV1 } from "./BiosJson";
import { Load } from "./StaticNodes";
import { CustomNode } from "./CustomNode";
LiteGraph.registered_node_types = {};
Load();

export const LoadAircraftNodes = (name: string, data: BiosAircraftV1) => {
  // console.log(name, data);
   for (const [categoryName, category] of Object.entries<BiosCategoryV1>(data)) {
    for (const [,control] of Object.entries<BiosControlV1>(category)) {
      for (const output of control.outputs) {
        if (output.type === "integer") {
          registerOutputNode(name, categoryName, output);
        }
      }
    }
   }
}

export class OutputNode extends CustomNode {
  aircraft: string;
  category: string;
  biosOutput: IntegerOutputV1;
  valueRaw?: number;
  value?: number;

  initOutputs() {
    if (this.biosOutput.type === "integer") {
      this.addOutput("Raw Value", "number");
      this.addOutput(`Max Value (${this.biosOutput.max_value})`, "number");
      this.addOutput("Value", "number");
    }
    this.size = this.computeSize();
    this.setDirtyCanvas(true, true);
  }

  updateOutputs() {
    this.setOutputData(0, this.valueRaw);
    this.setOutputData(1, this.biosOutput.max_value);
    this.setOutputData(2, this.value);
  }

  override onAdded(): void {
    this.size = this.computeSize();
    this.setDirtyCanvas(true, true);
  }

  override onDrawForeground(ctx: CanvasRenderingContext2D): void {
    if (this.valueRaw) this.writeOutputText(ctx, 0, `${this.valueRaw}`);
    if (this.value) this.writeOutputText(ctx, 2, `${this.value.toFixed(4)}`);
  }
}

const registerOutputNode = (aircraft: string, category: string, output: IntegerOutputV1) => {
  if (!output.address_identifier || output.type !== "integer") return;
  const NewNode = class extends OutputNode {
    title = output.address_identifier;
    aircraft = aircraft;
    category = category;
    biosOutput = output;
    constructor() {
      super();
      this.properties = {category, output};
      this.initOutputs();
    }
  };
  NewNode.title = output.address_identifier;
  const menuPath = `DCS/${aircraft}/${category}/${output.address_identifier}`;
  LiteGraph.registerNodeType(
    menuPath,
    NewNode
  );
}