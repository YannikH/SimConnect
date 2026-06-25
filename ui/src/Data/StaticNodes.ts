import { LGraphNode, LiteGraph } from "litegraph.js";
import { CustomNode } from "./CustomNode";
import { PostMessage } from "./WebviewHandler";

class TrcGeneralGaugeNode extends LGraphNode {
  title = "GeneralGauge";
  constructor() {
    super();
      const values = [...new Array(10)].map((_, index) => index);
      this.addWidget("combo","TRC Gauge", values[0], () => {}, { values, property: "gaugeIndex"} );
      this.addInput("Pointer1", "number");
      this.addInput("Pointer2", "number");
      this.addInput("Servo1", "number");
      this.addInput("Servo2", "number");
      this.addInput("Light", "number");
  }

  override onExecute(): void {
    const gaugeData = {
      gaugeIndex: this.properties["gaugeIndex"] ?? 0,
      // Servo1: Math.round(this.getInputData(2, true) ?? 0),
      // Servo2: Math.round(this.getInputData(3, true) ?? 0),
      light: Math.round(this.getInputData(4, true) ?? 0),
    }
    PostMessage({ type: "GaugeChanged", data: gaugeData});
  }
}
TrcGeneralGaugeNode.title = "GeneralGauge";

class MathNode extends CustomNode {
  override onExecute(): void {
    const a = this.getInputData(0, true);
    const b = this.getInputData(1, true);
    // console.log("EXECUTING MATH NODE", a, b, this);
    if (!a || !b) return;
    const result = this.calculate(a, b);
    // console.log(result);
    this.setOutputData(0, result);
    this.setDirtyCanvas(false, true);
  }
  override onDrawForeground(ctx: CanvasRenderingContext2D): void {
    const value = this.getOutputData(0) ?? 0;
    this.writeOutputText(ctx, 0, `${value.toFixed(4)}`);
  }
  calculate(a: number, b: number): number {
    return a + b;
  }
}

class Number extends LGraphNode {
  title = "Number";
  constructor() {
    super();
      this.addWidget("number","Value", this.properties["value"] ?? 0, (val: number) => {
        this.setProperty("value", val);
        this.setOutputData(0, val);
      }, {property: "value"});
      this.addOutput("Out", "number");
  }
  onExecute(): void {
    
    this.setOutputData(0, this.properties["value"]);
  }
}

class Add extends MathNode {
  title = "Add";
  constructor() {
    super();
      this.addInput("A", "number");
      this.addInput("B", "number");
      this.addOutput("Out", "number");
  }
}

class Subtract extends MathNode {
  title = "Subtract";
  constructor() {
    super();
      this.addInput("A", "number");
      this.addInput("B", "number");
      this.addOutput("Out", "number");
  }
  calculate(a: number, b: number): number {
    return a - b;
  }
}

class Divide extends MathNode {
  title = "Divide";
  constructor() {
    super();
      this.addInput("A", "number");
      this.addInput("B", "number");
      this.addOutput("Out", "number");
  }
  calculate(a: number, b: number): number {
    return a / b;
  }
}

class Multiply extends MathNode {
  title = "Multiply";
  constructor() {
    super();
      this.addInput("A", "number");
      this.addInput("B", "number");
      this.addOutput("Out", "number");
  }
  calculate(a: number, b: number): number {
    return a * b;
  }
}

class Clamp extends MathNode {
  title = "Clamp";
  constructor() {
    super();
      this.addInput("In", "number");
      this.addInput("Min", "number");
      this.addInput("Max", "number");
      this.addOutput("Out", "number");
  }
  override onExecute(): void {
    const inVal = this.getInputData(0, true);
    const min = this.getInputData(1, true);
    const max = this.getInputData(2, true);
    if (!inVal || !min || !max) return;
    const outVal = Math.max(Math.min(inVal, max), min);
    this.setOutputData(0, outVal);
    this.setDirtyCanvas(false, true);
  }
}


export const Load = () => {
  LiteGraph.registerNodeType(
    "TRC/GeneralGauge",
    TrcGeneralGaugeNode
  );
  LiteGraph.registerNodeType(
    "Math/Number",
    Number
  );
  LiteGraph.registerNodeType(
    "Math/Add",
    Add
  );
  LiteGraph.registerNodeType(
    "Math/Subtract",
    Subtract
  );
  LiteGraph.registerNodeType(
    "Math/Divide",
    Divide
  );
  LiteGraph.registerNodeType(
    "Math/Multiply",
    Multiply
  );
  LiteGraph.registerNodeType(
    "Math/Clamp",
    Clamp
  );
};