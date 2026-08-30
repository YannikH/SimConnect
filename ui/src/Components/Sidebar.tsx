import { IconButton, Typography } from "@mui/material";
import { Flex } from "./Structure";
import { PAGES, type PageId } from "../Data/Pages";

type SidebarProps = {
  activePage: PageId;
  onNavigate: (page: PageId) => void;
  graphNames: string[];
  loadedName: string;
  onSelectGraph: (name: string) => void;
  onRefresh: () => void;
};

const NavItem = ({
  label,
  active,
  onClick,
}: {
  label: string;
  active: boolean;
  onClick: () => void;
}) => (
  <div
    onClick={onClick}
    style={{
      padding: "6px 8px",
      cursor: "pointer",
      fontWeight: active ? 600 : 400,
      background: active ? "rgba(128,128,128,0.3)" : "transparent",
    }}
  >
    {label}
  </div>
);

const Sidebar = ({
  activePage,
  onNavigate,
  graphNames,
  loadedName,
  onSelectGraph,
  onRefresh,
}: SidebarProps) => {
  return (
    <Flex $column $fullHeight style={{ width: 200, borderRight: "1px solid #444" }}>
      <Flex $column style={{ borderBottom: "1px solid #444", padding: "4px 0" }}>
        {PAGES.map((page) => (
          <NavItem
            key={page.id}
            label={page.label}
            active={page.id === activePage}
            onClick={() => onNavigate(page.id)}
          />
        ))}
      </Flex>

      {activePage === "graphs" && (
        <>
          <Flex $row style={{ alignItems: "center", justifyContent: "space-between", paddingRight: 4 }}>
            <Typography variant="h6" style={{ padding: "0 5px" }}>
              Graphs
            </Typography>
            <IconButton size="small" onClick={onRefresh} title="Refresh graph list">
              ⟳
            </IconButton>
          </Flex>
          <Flex $column $grow style={{ overflowY: "auto" }}>
            {graphNames.length === 0 && (
              <div style={{ padding: "8px", opacity: 0.6 }}>No saved graphs</div>
            )}
            {graphNames.map((name) => (
              <div
                key={name}
                onClick={() => onSelectGraph(name)}
                title={name}
                style={{
                  padding: "6px 8px",
                  cursor: "pointer",
                  whiteSpace: "nowrap",
                  overflow: "hidden",
                  textOverflow: "ellipsis",
                  background: name === loadedName ? "rgba(128,128,128,0.3)" : "transparent",
                }}
              >
                {name}
              </div>
            ))}
          </Flex>
        </>
      )}
    </Flex>
  );
};

export default Sidebar;
