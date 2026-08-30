import { LGraphNode, LiteGraph } from "litegraph.js";

export class CustomNode extends LGraphNode {
  // Not part of litegraph's typed API, but it calls this by duck-typed convention whenever any
  // widget's value changes (typing/dragging/toggling). Bubbling it up to graph.afterChange()
  // lets LitegraphManager re-send the graph to the C# side while a run is already active.
  onWidgetChanged(): void {
    this.graph?.afterChange();
  }

  writeOutputText(ctx: CanvasRenderingContext2D, slot: number, value: string) {
    const initialStroke = ctx.strokeStyle;
    const initialFill = ctx.fillStyle;
    const margin = 2;
    const xOffset = this.size[0];
    const lineHeight = LiteGraph.NODE_SLOT_HEIGHT;
    const y = lineHeight * (slot );
    ctx.fillRect(
      xOffset,
      y,
      ctx.measureText(value).width + (margin * 4),
      lineHeight + (margin * 2)
    )
    ctx.strokeStyle = "black";
    ctx.strokeRect(
      xOffset,
      y,
      ctx.measureText(value).width + (margin * 4),
      lineHeight + (margin * 2)
    )
    ctx.fillStyle = "black";
    ctx.fillText(`${value}`, xOffset + (margin * 2), y + lineHeight - margin);

    ctx.strokeStyle = initialStroke;
    ctx.fillStyle = initialFill;
  }
}