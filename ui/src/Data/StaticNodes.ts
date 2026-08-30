import { LGraphNode, LiteGraph } from "litegraph.js";
import { CustomNode } from "./CustomNode";
import { PostMessage } from "./WebviewHandler";

// Gauges are addressed by their USB product ID rather than an arbitrary connection-order index.
const addGaugeIdWidget = (node: LGraphNode) => {
  node.addWidget("number", "Gauge ID", node.properties["gaugeId"] ?? 0, (id: number) => {
        node.setProperty("gaugeId", id);
      }, {
    property: "gaugeId",
    precision: 1,
  });
};

// None of these node classes compute or send anything themselves anymore - the graph only
// ever runs on the C# side (GraphRunner). They stay purely structural (inputs/outputs/widgets)
// so they still appear in the editor and can be wired up; their displayed values come from
// window.dcs.setNodeValues pushes, via the ordinary getOutputData()-reading onDrawForeground below.

class TrcGeneralGaugeNode extends CustomNode {
  title = "GeneralGauge";
  constructor() {
    super();
    addGaugeIdWidget(this);
    this.addInput("Servo1", "number");
    this.addInput("Servo2", "number");
    this.addInput("Light", "number");
  }
}
TrcGeneralGaugeNode.title = "GeneralGauge";

class TrcAltimeterNode extends CustomNode {
  title = "Altimeter";
  constructor() {
    super();
    addGaugeIdWidget(this);
    this.addInput("Altitude (ft)", "number");
    this.addInput("Light", "number");
    this.addWidget("button", "+1000ft", undefined, () => {
      this.sendAdjust(1);
    });
    this.addWidget("button", "-1000ft", undefined, () => {
      this.sendAdjust(-1);
    });
  }

  // A manual one-off action, independent of graph execution (which only ever runs in C#).
  sendAdjust(adjust: number) {
    const gaugeData = {
      gaugeType: "AltimeterAdjust",
      gaugeId: Math.round(this.properties["gaugeId"] ?? 0),
      AltFt: Math.round(this.getInputData(0, true) ?? 0),
      light: Math.round(this.getInputData(1, true) ?? 0),
      adjust,
    };
    PostMessage({ type: "GaugeChanged", data: gaugeData });
  }
}
TrcAltimeterNode.title = "Altimeter";

class TrcHeadingIndicatorNode extends CustomNode {
  title = "HeadingIndicator";
  constructor() {
    super();
    addGaugeIdWidget(this);
    this.addInput("Direction", "number");
    this.addInput("Light", "number");
  }
}
TrcHeadingIndicatorNode.title = "HeadingIndicator";

class MathNode extends CustomNode {
  override onDrawForeground(ctx: CanvasRenderingContext2D): void {
    const value = this.getOutputData(0) ?? 0;
    this.writeOutputText(ctx, 0, `${value.toFixed(4)}`);
  }
}

class Number extends CustomNode {
  title = "Number";
  constructor() {
    super();
    this.addWidget("number", "Value", this.properties["value"] ?? 0, (val: number) => {
      this.setProperty("value", val);
    }, { property: "value" });
    this.addOutput("Out", "number");
  }
  override onDrawForeground(ctx: CanvasRenderingContext2D): void {
    const value = this.getOutputData(0) ?? 0;
    this.writeOutputText(ctx, 0, `${value.toFixed(4)}`);
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
}

class Divide extends MathNode {
  title = "Divide";
  constructor() {
    super();
      this.addInput("A", "number");
      this.addInput("B", "number");
      this.addOutput("Out", "number");
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
}

class Eval extends CustomNode {
  title = "Eval";
  constructor() {
    super();
    this.addInput("A", "number");
    this.addInput("B", "number");
    this.addOutput("Out", "number");
    this.properties = { operation: "a * 2" };
    this.addWidget(
      "text",
      "operation",
      this.properties["operation"],
      (val: string) => this.setProperty("operation", val),
      { property: "operation" }
    );
  }
  override onDrawForeground(ctx: CanvasRenderingContext2D): void {
    const value = this.getOutputData(0) ?? 0;
    this.writeOutputText(ctx, 0, `${value.toFixed(4)}`);
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
}


export const Load = () => {
  LiteGraph.registerNodeType(
    "TRC/GeneralGauge",
    TrcGeneralGaugeNode
  );
  LiteGraph.registerNodeType(
    "TRC/Altimeter",
    TrcAltimeterNode
  );
  LiteGraph.registerNodeType(
    "TRC/HeadingIndicator",
    TrcHeadingIndicatorNode
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
  LiteGraph.registerNodeType(
    "Math/Eval",
    Eval
  );
};
