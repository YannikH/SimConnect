import { LGraphNode, LiteGraph } from "litegraph.js";

export class CustomNode extends LGraphNode {
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