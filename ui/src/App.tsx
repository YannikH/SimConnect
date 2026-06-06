import { useState } from "react";
import {
  BiosAircraftSchemaV1,
  BiosConfigSchemaV1,
  buildOutputsByAddress,
  type BiosConfigV1,
  type OutputMatch,
} from "./Data/BiosJson";
import { Flex } from "./Components/Structure";
import GraphEditor from "./Components/GraphEditor";
import { LoadAircraftNodes } from "./Data/NodeLoader";

declare global {
  interface Window {
    chrome: {
      webview:
        | {
            postMessage: (content: unknown) => void;
          }
        | undefined;
    };
    biosConfigs: BiosConfigV1;
    dcs: {
      setData: (address: number, data: number) => void;
      onConfig: (name: string, data: unknown) => void;
    };
  }
}

const onConfig = (name: string, data: unknown) => {
  const result = BiosAircraftSchemaV1.safeParse(data);
  if (!result.success) {
    console.log("Failed to decode", name, data, result);
    return;
  }
  LoadAircraftNodes(name, result.data);
};

window.dcs = {
  setData: console.log,
  onConfig: onConfig,
};

if (window.chrome.webview) {
  window.chrome.webview.postMessage({ type: "PageLoaded" });
}

type biosData = { [key: string]: number };

function App() {
  const [config, setConfig] = useState<BiosConfigV1 | undefined>();
  const [biosData, setBiosData] = useState<biosData>({});
  const [outputMap, setOutputMap] = useState<Record<number, OutputMatch[]>>({});
  const attemptParse = () => {
    const parseResult = BiosConfigSchemaV1.safeParse(window.biosConfigs);
    if (parseResult.success) {
      setConfig(parseResult.data);
      setOutputMap(buildOutputsByAddress(parseResult.data));
      console.log(buildOutputsByAddress(parseResult.data));
    }
  };

  const dataChange = (address: number, data: number) => {
    console.log(address, data);
    const outputs = outputMap[address];
    const newBiosData = { ...biosData };
    for (const output of outputs) {
      if (output.output.type === "integer") {
        newBiosData[output.controlIdentifier] = data & output.output.mask;
      } else {
        newBiosData[output.controlIdentifier] = data;
      }
    }
    setBiosData(newBiosData);
  };

  // eslint-disable-next-line react-hooks/immutability
  window.dcs.setData = dataChange;

  return (
    <Flex $column $fullHeight $fullWidth>
      <GraphEditor />
      {/* <button onClick={attemptParse}>ATTEMPT PARSE</button>
      <Flex $row $grow $hideOverflow>
        {config && <ConfigView config={config} />}
        <Flex $column $scroll>
          Data:
          {Object.keys(biosData).map((k) => (
            <>
              {k}: {biosData[k]}
              <br />
            </>
          ))}
        </Flex>
      </Flex> */}
    </Flex>
  );
}

export default App;
