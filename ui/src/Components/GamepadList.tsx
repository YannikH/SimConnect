import { useEffect, useRef, useState } from "react";
import { Card, CardContent, LinearProgress, Stack, Typography } from "@mui/material";
import { Flex } from "./Structure";

type GamepadSnapshot = {
  index: number;
  id: string;
  axes: number[];
  buttons: { pressed: boolean; value: number }[];
};

const readGamepads = (): GamepadSnapshot[] => {
  const pads = navigator.getGamepads?.() ?? [];
  const result: GamepadSnapshot[] = [];
  for (const pad of pads) {
    if (!pad) continue;
    result.push({
      index: pad.index,
      id: pad.id,
      axes: Array.from(pad.axes),
      buttons: pad.buttons.map((b) => ({ pressed: b.pressed, value: b.value })),
    });
  }
  return result;
};

const AxisRow = ({ label, value }: { label: string; value: number }) => (
  <Stack direction="row" sx={{ alignItems: "center", gap: 1 }}>
    <Typography variant="caption" color="text.secondary" sx={{ width: 24 }}>
      {label}
    </Typography>
    <LinearProgress
      variant="determinate"
      value={((value + 1) / 2) * 100}
      sx={{
        flexGrow: 1,
        height: 6,
        borderRadius: 1,
        "& .MuiLinearProgress-bar": { transition: "none" },
      }}
    />
    <Typography variant="caption" sx={{ fontFamily: "monospace", width: 40, textAlign: "right" }}>
      {value.toFixed(2)}
    </Typography>
  </Stack>
);

const ButtonBadge = ({ index, button }: { index: number; button: { pressed: boolean; value: number } }) => (
  <div
    title={`Button ${index}: ${button.value.toFixed(2)}`}
    style={{
      minWidth: 22,
      padding: "2px 6px",
      textAlign: "center",
      borderRadius: 4,
      fontFamily: "monospace",
      fontSize: 20,
      background: button.pressed ? "rgba(76, 175, 80, 0.6)" : "rgba(128,128,128,0.2)",
      border: "1px solid rgba(255,255,255,0.2)",
    }}
  >
    {index}
  </div>
);

const GamepadCard = ({ gamepad }: { gamepad: GamepadSnapshot }) => (
  <Card variant="outlined" sx={{ width: "100%" }}>
    <CardContent sx={{ "&:last-child": { paddingBottom: 2 } }}>
      <Typography variant="subtitle2" gutterBottom sx={{ wordBreak: "break-word" }}>
        #{gamepad.index} &mdash; {gamepad.id}
      </Typography>
      <Flex $row $fullWidth>
        <Flex $column $width="50%">
          <Typography variant="body2" sx={{ mt: 1 }}>
            Axes
          </Typography>
          <Stack spacing={0.5} sx={{ mb: 1 }}>
            {gamepad.axes.map((value, i) => (
              <AxisRow key={i} label={`A${i}`} value={value} />
            ))}
          </Stack>
        </Flex>
        <Flex $column $padding="0 30px">
          <Typography variant="body2">Buttons</Typography>
          <Flex $row $wrap $gap="4px">
            {gamepad.buttons.map((button, i) => (
              <ButtonBadge key={i} index={i} button={button} />
            ))}
          </Flex>
        </Flex>
      </Flex>

    </CardContent>
  </Card>
);

const GamepadList = () => {
  const [gamepads, setGamepads] = useState<GamepadSnapshot[]>([]);
  const frameRef = useRef<number>(0);

  useEffect(() => {
    const tick = () => {
      setGamepads(readGamepads());
      frameRef.current = requestAnimationFrame(tick);
    };
    frameRef.current = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(frameRef.current);
  }, []);

  return (
    <Flex $column $wrap $gap="8px" style={{ minWidth: "260px" }}>
      <Typography variant="h6">Gamepads</Typography>
      {gamepads.length === 0 && (
        <Typography variant="body2" color="text.secondary">
          No gamepads connected. If one is plugged in, press a button on it once
          &mdash; browsers only report a gamepad after it sees input.
        </Typography>
      )}
      {gamepads.map((gamepad) => (
        <GamepadCard key={gamepad.index} gamepad={gamepad} />
      ))}
    </Flex>
  );
};

export default GamepadList;
