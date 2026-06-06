import * as z from "zod";

// Inputs

export const FixedStepInputSchemaV1 = z.object({
  description: z.string(),
  interface: z.literal("fixed_step"),
});
export type FixedStepInputV1 = z.infer<typeof FixedStepInputSchemaV1>;

export const SetStateInputSchemaV1 = z.object({
  description: z.string(),
  interface: z.literal("set_state"),
  max_value: z.number(),
});
export type SetStateInputV1 = z.infer<typeof SetStateInputSchemaV1>;

export const ActionInputSchemaV1 = z.object({
  argument: z.string(),
  description: z.string(),
  interface: z.literal("action"),
});
export type ActionInputV1 = z.infer<typeof ActionInputSchemaV1>;

export const VariableStepInputSchemaV1 = z.object({
  description: z.string(),
  interface: z.literal("variable_step"),
  max_value: z.number(),
  suggested_step: z.number(),
});
export type VariableStepInputV1 = z.infer<typeof VariableStepInputSchemaV1>;

export const SetStringInputSchemaV1 = z.object({
  description: z.string(),
  interface: z.literal("set_string"),
});
export type SetStringInputV1 = z.infer<typeof SetStringInputSchemaV1>;

export const BiosInputSchemaV1 = z.discriminatedUnion("interface", [
  FixedStepInputSchemaV1,
  SetStateInputSchemaV1,
  ActionInputSchemaV1,
  VariableStepInputSchemaV1,
  SetStringInputSchemaV1,
]);
export type BiosInputV1 = z.infer<typeof BiosInputSchemaV1>;

// Outputs

export const IntegerOutputSchemaV1 = z.object({
  address: z.number(),
  address_identifier: z.string().optional(),
  address_mask_identifier: z.string().optional(),
  address_mask_shift_identifier: z.string().optional(),
  description: z.string(),
  mask: z.number(),
  max_value: z.number(),
  shift_by: z.number(),
  suffix: z.string(),
  type: z.literal("integer"),
});
export type IntegerOutputV1 = z.infer<typeof IntegerOutputSchemaV1>;

export const StringOutputSchemaV1 = z.object({
  address: z.number(),
  address_identifier: z.string().optional(),
  description: z.string(),
  max_length: z.number(),
  suffix: z.string(),
  type: z.literal("string"),
});
export type StringOutputV1 = z.infer<typeof StringOutputSchemaV1>;

export const BiosOutputSchemaV1 = z.discriminatedUnion("type", [
  IntegerOutputSchemaV1,
  StringOutputSchemaV1,
]);
export type BiosOutputV1 = z.infer<typeof BiosOutputSchemaV1>;


const BiosControlSchemaV1 = z.object({
  api_variant: z.string().optional(),
  category: z.string(),
  control_type: z.string(),
  description: z.string(),
  identifier: z.string(),
  inputs: z.array(BiosInputSchemaV1),
  outputs: z.array(BiosOutputSchemaV1),
});
export type BiosControlV1 = z.infer<typeof BiosControlSchemaV1>;


export const BiosCategorySchemaV1 = z.record(z.string(), BiosControlSchemaV1);
export type BiosCategoryV1 = z.infer<typeof BiosCategorySchemaV1>;


export const BiosAircraftSchemaV1 = z.record(z.string(), BiosCategorySchemaV1);
export type BiosAircraftV1 = z.infer<typeof BiosAircraftSchemaV1>;

// Unsure what to use this for
export const AircraftAliasesSchemaV1 = z.record(z.string(), z.array(z.string()));
export type AircraftAliasesV1 = z.infer<typeof AircraftAliasesSchemaV1>;


export const BiosConfigSchemaV1 = z.record(
  z.string(),
  z.union([BiosAircraftSchemaV1, AircraftAliasesSchemaV1])
);
export type BiosConfigV1 = z.infer<typeof BiosConfigSchemaV1>;

export interface OutputMatch {
  aircraft: string;
  category: string;
  controlIdentifier: string;
  output: BiosOutputV1;
}

export function buildOutputsByAddress(
  config: BiosConfigV1
): Record<number, OutputMatch[]> {
  const map: Record<number, OutputMatch[]> = {};
  for (const [aircraft, aircraftConfig] of Object.entries(config)) {
    for (const [category, controls] of Object.entries(aircraftConfig as BiosAircraftV1)) {
      if (typeof controls !== "object" || Array.isArray(controls)) continue;
      for (const control of Object.values(controls)) {
        for (const output of control.outputs) {
          const match: OutputMatch = { aircraft, category, controlIdentifier: control.identifier, output };
          (map[output.address] ??= []).push(match);
        }
      }
    }
  }
  return map;
}

export function findOutputsByAddress(
  config: BiosConfigV1,
  address: number
): OutputMatch[] {
  const results: OutputMatch[] = [];
  for (const [aircraft, aircraftConfig] of Object.entries(config)) {
    for (const [category, controls] of Object.entries(aircraftConfig as BiosAircraftV1)) {
      if (typeof controls !== "object" || Array.isArray(controls)) continue;
      for (const control of Object.values(controls)) {
        for (const output of control.outputs) {
          if (output.address === address) {
            results.push({ aircraft, category, controlIdentifier: control.identifier, output });
          }
        }
      }
    }
  }
  return results;
}


export const exampleOutputs: BiosOutputV1[] = [
  {
    "address": 17596,
    "address_identifier": "F_16C_50_ADI_BANK_A",
    "address_mask_shift_identifier": "F_16C_50_ADI_BANK",
    "description": "gauge position",
    "mask": 65535,
    "max_value": 65535,
    "shift_by": 0,
    "suffix": "",
    "type": "integer"
  },
  {
    "address": 17594,
    "address_identifier": "F_16C_50_ADI_PITCH_A",
    "address_mask_shift_identifier": "F_16C_50_ADI_PITCH",
    "description": "gauge position",
    "mask": 65535,
    "max_value": 65535,
    "shift_by": 0,
    "suffix": "",
    "type": "integer"
  }
]