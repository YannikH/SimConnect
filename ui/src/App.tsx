import {
  BiosAircraftSchemaV1,
} from "./Data/BiosJson";
import { Flex } from "./Components/Structure";
import GraphEditor from "./Components/GraphEditor";
import GaugeList from "./Components/GaugeList";
import { LoadAircraftNodes } from "./Data/NodeLoader";

const DCS_FILENAMES_KEY = 'DCS_FILE_NAMES';
declare global {
  interface Window {
    chrome: {
      webview:
        | {
            postMessage: (content: unknown) => void;
          }
        | undefined;
    };
    dcs: {
      setData: (address: number, data: number) => void;
      onBiosConfig: (name: string, data: unknown) => void;
    };
    trc: {
      setGauges: (data: unknown) => void;
    };
  }
}

const getNamesList = (): string[] => {
  try {
    const namesList = JSON.parse(localStorage.getItem(DCS_FILENAMES_KEY) ?? '[]');
    return Array.isArray(namesList) ? namesList : [];
  } catch {
    console.log('Names list could not be parsed');
    return [];
  }
};

const saveConfigName = (name: string) => {
  const namesList = getNamesList();
  const newList = [...new Set([...namesList, name])];
  localStorage.setItem(DCS_FILENAMES_KEY, JSON.stringify(newList));
};

const onBiosConfig = (name: string, data: unknown, save = true) => {
  console.log('loading bios config', name, data);
  if (name === 'AircraftAliases') return;
  if (save && name.includes('FA-18') || name.includes('F-16')) {
    saveConfigName(name);
    localStorage.setItem(name, JSON.stringify(data));
  } else {
    localStorage.setItem(name, '');
  }
  const result = BiosAircraftSchemaV1.safeParse(data);
  if (!result.success) {
    console.log("Failed to decode", name, data, result);
    return;
  }
  LoadAircraftNodes(name, result.data);
};

const loadBiosConfigCache = () => {
  for (const name of getNamesList()) {
    try {
      const configObject = JSON.parse(localStorage.getItem(name) ?? '');
      console.log('Loading bios config nodes from cache: ', name, configObject);
      onBiosConfig(name, configObject, false);
    } catch (e){
      console.log('Config could not be parsed', name, e);
    }
  }
};

window.dcs = {
  setData: console.log,
  onBiosConfig: onBiosConfig,
};

window.trc = {
  setGauges: console.log,
}

if (window.chrome.webview) {
  window.chrome.webview.postMessage({ type: "PageLoaded" });
}

function App() {
  loadBiosConfigCache();
  return (
    <Flex $column $fullHeight $fullWidth>
      <Flex $row $fullHeight>
        <GraphEditor />
        <Flex $column>
          <GaugeList />
        </Flex>
      </Flex>
    </Flex>
  );
}

export default App;
