import { useEffect, useRef } from "react";
import { Flex } from "./Structure";

import "litegraph.js/css/litegraph.css";
import { Button, styled } from "@mui/material";
import { litegraphManager } from "../Data/LitegraphManager";

const TopBarButton = styled(Button)`
  border: solid 1px white;
  font-family: monospace;
  background-color: #283f51;
  padding: 0 5px;
  color: white;
`;

type GraphEditorProps = {
  loadedName: string;
};

const GraphEditor = ({ loadedName }: GraphEditorProps) => {
  const canvasRef = useRef<HTMLCanvasElement | null>(null);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    litegraphManager.startCanvas(canvas);
    return () => litegraphManager.stop();
  }, []);

  return (
    <Flex $column $grow $hideOverflow $fullHeight>
      <Flex $row $fullWidth style={{ height: "30px", gap: "8px", alignItems: "center", padding: "0 8px" }}>
        <TopBarButton onClick={() => litegraphManager.loadGraphDialog()} variant="contained">
          Load
        </TopBarButton>
        <TopBarButton onClick={() => litegraphManager.saveGraphDialog()} variant="contained">
          Save As
        </TopBarButton>
        {loadedName && <span>{loadedName}</span>}
      </Flex>
      <Flex $grow $hideOverflow $fullWidth>
        <Flex $fullWidth $fullHeight>
          <canvas ref={canvasRef} width="1024" height="720" />
        </Flex>
      </Flex>
    </Flex>
  );
};

export default GraphEditor;
