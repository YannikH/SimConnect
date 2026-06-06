import { useState } from "react";
import Box from "@mui/material/Box";
import Chip from "@mui/material/Chip";
import Divider from "@mui/material/Divider";
import List from "@mui/material/List";
import ListItemButton from "@mui/material/ListItemButton";
import ListItemText from "@mui/material/ListItemText";
import ListSubheader from "@mui/material/ListSubheader";
import Paper from "@mui/material/Paper";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import type {
  BiosAircraftV1,
  BiosConfigV1,
  BiosControlV1,
  BiosOutputV1,
} from "../Data/BiosJson";
import { Flex } from "./Structure";

function isAircraftData(value: unknown): value is BiosAircraftV1 {
  if (typeof value !== "object" || value === null || Array.isArray(value))
    return false;
  const firstVal = Object.values(value as object)[0];
  return typeof firstVal === "object" && !Array.isArray(firstVal);
}

function OutputRow({ output }: { output: BiosOutputV1 }) {
  const rows: [string, string][] =
    output.type === "integer"
      ? [
          ["address", String(output.address)],
          ["type", output.type],
          ["mask", `0x${output.mask.toString(16).toUpperCase()}`],
          ["max_value", String(output.max_value)],
          ["shift_by", String(output.shift_by)],
        ]
      : [
          ["address", String(output.address)],
          ["type", output.type],
          ["max_length", String(output.max_length)],
        ];

  return (
    <Paper variant="outlined" sx={{ px: 1.5, py: 1 }}>
      <Box
        sx={{
          display: "grid",
          gridTemplateColumns: "1fr 1fr",
          columnGap: 2,
          rowGap: 0.5,
        }}
      >
        {rows.map(([key, val]) => (
          <>
            <Typography key={`k-${key}`} variant="caption">
              {key}
            </Typography>
            <Typography key={`v-${key}`}>{val}</Typography>
          </>
        ))}
        <textarea value={JSON.stringify(output, null, 2)}></textarea>
      </Box>
    </Paper>
  );
}

function ControlCard({ control }: { control: BiosControlV1 }) {
  return (
    <Box sx={{ py: 1.75 }}>
      <Stack direction="row" spacing={1} sx={{ mb: 1 }}>
        <Typography variant="body2">{control.description}</Typography>
        <Typography variant="caption" component="code" color="text.secondary">
          {control.identifier}
        </Typography>
        <Chip
          label={control.control_type}
          size="small"
          variant="outlined"
          color="primary"
        />
      </Stack>
      {control.outputs.length === 0 ? (
        <Typography variant="caption" color="text.secondary">
          no outputs
        </Typography>
      ) : (
        <Stack spacing={0.75}>
          {control.outputs.map((out, i) => (
            <OutputRow key={i} output={out} />
          ))}
        </Stack>
      )}
      <Divider sx={{ mt: 1.75 }} />
    </Box>
  );
}

interface Props {
  config: BiosConfigV1;
}

export default function ConfigView({ config }: Props) {
  const [selectedAircraft, setSelectedAircraft] = useState<string | null>(null);
  const [selectedCategory, setSelectedCategory] = useState<string | null>(null);

  const aircraftEntries = Object.entries(config).filter(([, v]) =>
    isAircraftData(v)
  ) as [string, BiosAircraftV1][];

  const aircraft = selectedAircraft
    ? (config[selectedAircraft] as BiosAircraftV1)
    : null;

  const categories = aircraft ? Object.keys(aircraft) : [];

  const controls =
    aircraft && selectedCategory
      ? Object.values(aircraft[selectedCategory])
      : [];

  return (
    <Flex $row $grow>
      {/* Aircraft list */}
      <List
        dense
        disablePadding
        subheader={<ListSubheader disableSticky>Aircraft</ListSubheader>}
        sx={{ borderRight: 1, borderColor: "divider", overflowY: "auto" }}
      >
        {aircraftEntries.map(([filename]) => (
          <ListItemButton
            key={filename}
            selected={selectedAircraft === filename}
            onClick={() => {
              setSelectedAircraft(filename);
              setSelectedCategory(null);
            }}
          >
            <ListItemText
              primary={filename.replace(/\.json$/, "")}
              slotProps={{ primary: { variant: "body2", noWrap: true } }}
            />
          </ListItemButton>
        ))}
      </List>

      {/* Category list */}
      <List
        dense
        disablePadding
        subheader={<ListSubheader disableSticky>Category</ListSubheader>}
        sx={{ borderRight: 1, borderColor: "divider", overflowY: "auto" }}
      >
        {categories.map((cat) => (
          <ListItemButton
            key={cat}
            selected={selectedCategory === cat}
            onClick={() => setSelectedCategory(cat)}
          >
            <ListItemText
              primary={cat}
              slotProps={{ primary: { variant: "body2", noWrap: true } }}
            />
          </ListItemButton>
        ))}
      </List>

      {/* Controls / outputs */}
      <Box sx={{ overflowY: "auto", px: 2.5 }}>
        {controls.length === 0 ? (
          <Box
            sx={{
              display: "flex",
              height: "100%",
              alignItems: "center",
              justifyContent: "center",
            }}
          >
            <Typography variant="body2" color="text.secondary">
              {!selectedAircraft
                ? "Select an aircraft to begin"
                : !selectedCategory
                ? "Select a category"
                : "No controls in this category"}
            </Typography>
          </Box>
        ) : (
          controls.map((control) => (
            <ControlCard key={control.identifier} control={control} />
          ))
        )}
      </Box>
    </Flex>
  );
}
