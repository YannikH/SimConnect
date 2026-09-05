import { useEffect, useState } from "react";
import {
  BiosAircraftSchemaV1,
} from "./Data/BiosJson";
import { Flex } from "./Components/Structure";
import GraphEditor from "./Components/GraphEditor";
import GaugeList from "./Components/GaugeList";
import Sidebar from "./Components/Sidebar";
import { LoadAircraftNodes } from "./Data/NodeLoader";
import GamepadList from "./Components/GamepadList";
import DebugLog from "./Components/DebugLog";
import { litegraphManager } from "./Data/LitegraphManager";
import { debugLogManager } from "./Data/DebugLogManager";
import { gaugeListManager } from "./Data/GaugeListManager";
import type { PageId } from "./Data/Pages";

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
      setGraphList: (names: string[]) => void;
      onGraphLoaded: (name: string, data: unknown) => void;
      setNodeValues: (values: Array<[number, number, number]>) => void;
      onGraphDeactivated: () => void;
    };
    trc: {
      setGauges: (data: unknown) => void;
      onGaugeUpdate: (data: unknown) => void;
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

// Additive, like every other module that touches window.dcs - LitegraphManager (imported
// below, via GraphEditor/Sidebar) already installs the real setGraphList/onGraphLoaded/
// setNodeValues/onGraphDeactivated handlers before this line runs, since ES module imports
// fully evaluate before this module's own top-level code does. A plain replacing object
// literal here would silently reset those back to no-op stubs.
window.dcs = {
  ...window.dcs,
  setData: console.log,
  onBiosConfig: onBiosConfig,
};

window.trc = {
  setGauges: console.log,
  onGaugeUpdate: console.log,
}

if (window.chrome.webview) {
  window.chrome.webview.postMessage({ type: "PageLoaded" });
}

function App() {
  loadBiosConfigCache();

  const [graphNames, setGraphNames] = useState<string[]>([]);
  const [loadedName, setLoadedName] = useState<string>("");
  const [activePage, setActivePage] = useState<PageId>("graphs");

  useEffect(() => {
    litegraphManager.setGraphListListener(setGraphNames);
    litegraphManager.setGraphLoadedListener(setLoadedName);
    debugLogManager.attach();
    gaugeListManager.attach();
  }, []);

  return (
    <Flex $column $fullHeight $fullWidth>
      <Flex $row $fullHeight>
        <Sidebar
          activePage={activePage}
          onNavigate={setActivePage}
          graphNames={graphNames}
          loadedName={loadedName}
          onSelectGraph={(name) => litegraphManager.loadGraph(name)}
          onRefresh={() => litegraphManager.refreshGraphList()}
        />
        {activePage === "graphs" && (
          <>
          <GraphEditor loadedName={loadedName} />
          <Flex $grow $hideOverflow $fullHeight style={{ overflowY: "auto", padding: 8 }}>
            <GaugeList />
          </Flex>
          </>
        )}
        {activePage === "gamepads" && (
          <Flex $grow $hideOverflow $fullHeight style={{ overflowY: "auto", padding: 8 }}>
            <GamepadList />
          </Flex>
        )}
        {activePage === "debug" && (
          <Flex $grow $hideOverflow $fullHeight>
            <DebugLog />
          </Flex>
        )}
      </Flex>
    </Flex>
  );
}

export default App;
