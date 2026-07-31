import { useEffect, useState } from "react";
import { Card, CardContent, Stack, Typography } from "@mui/material";
import { Flex } from "./Structure";
import { GaugeListSchemaV1, type GaugeListV1, type GaugeV1 } from "../Data/GaugeData";

const formatValue = (value: number) =>
  `${value} (0x${value.toString(16).padStart(4, "0")})`;

type GaugeViewProps = {
  gauge: GaugeV1;
  index: number;
};

const GaugeField = ({ label, value }: { label: string; value: string }) => (
  <Stack direction="row" sx={{ justifyContent: "space-between", textWrap: "nowrap" }}>
    <Typography variant="caption" color="text.secondary">
      {label}
    </Typography>
    <Typography variant="body2" sx={{ fontFamily: "monospace" }}>
      {value}
    </Typography>
  </Stack>
);

export const GaugeView = ({ gauge, index }: GaugeViewProps) => {
  return (
    <Card variant="outlined" sx={{ width: "100%" }}>
      <CardContent sx={{ "&:last-child": { paddingBottom: 2 } }}>
        <Typography variant="subtitle2" gutterBottom>
          Gauge {index}
        </Typography>
        <Stack spacing={0.5}>
          <GaugeField label="Vendor ID" value={formatValue(gauge.vendorID)} />
          <GaugeField label="Product ID" value={formatValue(gauge.productID)} />
          <GaugeField label="Version" value={formatValue(gauge.versionNumber)} />
        </Stack>
      </CardContent>
    </Card>
  );
};

const GaugeList = () => {
  const [gauges, setGauges] = useState<GaugeListV1>([]);

  useEffect(() => {
    window.trc = {
      ...window.trc,
      setGauges: (data: unknown) => {
        const result = GaugeListSchemaV1.safeParse(data);
        if (!result.success) {
          console.error("Failed to parse gauge list", data, result.error);
          return;
        }
        setGauges(result.data);
      },
    };
  }, []);

  return (
    <Flex $column $wrap $gap="8px" style={{minWidth: "200px"}}>
        <Typography variant="h6">TRC Gauges</Typography>
      {gauges.map((gauge, index) => (
        <GaugeView key={index} gauge={gauge} index={index} />
      ))}
    </Flex>
  );
};

export default GaugeList;
