import { useEffect, useState } from "react";
import { Typography } from "@mui/material";
import { Flex } from "./Structure";
import { debugLogManager, type GaugeUpdateEntry } from "../Data/DebugLogManager";

const DebugLog = () => {
  const [entries, setEntries] = useState<GaugeUpdateEntry[]>([]);

  useEffect(() => {
    debugLogManager.setListener(setEntries);
    return () => debugLogManager.setListener(undefined);
  }, []);

  return (
    <Flex $column $fullHeight style={{ padding: 8, overflow: "hidden" }}>
      <Typography variant="h6">Gauge Update Log</Typography>
      <Typography variant="caption" color="text.secondary">
        Every gauge write actually sent to hardware, after rate limiting (most recent first).
      </Typography>
      <Flex
        $column
        $grow
        style={{ marginTop: 8, overflowY: "auto", fontFamily: "monospace", fontSize: 12 }}
      >
        {entries.length === 0 && <div style={{ opacity: 0.6 }}>No gauge updates yet</div>}
        {[...entries].reverse().map((entry, i) => (
          <div key={i} style={{ borderBottom: "1px solid #333", padding: "2px 0" }}>
            [{entry.time}] {entry.gaugeType} #{entry.gaugeId} {JSON.stringify(entry.data)}
          </div>
        ))}
      </Flex>
    </Flex>
  );
};

export default DebugLog;
