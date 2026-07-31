import * as z from "zod";

export const GaugeSchemaV1 = z.object({
  productID: z.number(),
  vendorID: z.number(),
  versionNumber: z.number(),
});
export type GaugeV1 = z.infer<typeof GaugeSchemaV1>;

export const GaugeListSchemaV1 = z.array(GaugeSchemaV1);
export type GaugeListV1 = z.infer<typeof GaugeListSchemaV1>;
