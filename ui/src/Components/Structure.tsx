import styled from "@emotion/styled";
import type { DetailedHTMLProps } from "react";

export type FlexProps = {
  $column?: boolean;
  $row?: boolean;
  $wrap?: boolean;
  $center?: boolean;
  $end?: boolean;
  $endContent?: boolean;
  $grow?: boolean;
  $padding?: string;
  $margin?: string;
  $border?: string;
  $height?: string;
  $width?: string;
  $spaceAround?: boolean;
  $centerContent?: boolean;
  $spaceBetween?: boolean;
  $fullWidth?: boolean;
  $fullHeight?: boolean;
  $hideOverflow?: boolean;
  $scroll?: boolean;
  $flex?: string;
  $gap?: string;
} & DetailedHTMLProps<React.HTMLAttributes<HTMLDivElement>, HTMLDivElement>;
export const Flex = styled.div<FlexProps>`
  display: flex;
  ${(props) => props.$column && { flexDirection: "column" }};
  ${(props) => props.$row && { flexDirection: "row" }};
  ${(props) => props.$wrap && { flexWrap: "wrap" }};
  ${(props) => props.$center && { alignItems: "center" }};
  ${(props) => props.$end && { alignItems: "flex-end" }};
  ${(props) => props.$endContent && { justifyContent: "end" }};
  ${(props) => props.$grow && { flexGrow: 1 }};
  ${(props) => props.$padding && { padding: props.$padding }};
  ${(props) => props.$margin && { margin: props.$margin }};
  ${(props) => props.$border && { padding: props.$border }};
  ${(props) => props.$height && { padding: props.$height }};
  ${(props) => props.$width && { width: props.$width }};
  ${(props) => props.$spaceAround && { justifyContent: "space-around" }};
  ${(props) => props.$spaceBetween && { justifyContent: "space-between" }};
  ${(props) => props.$centerContent && { justifyContent: "center" }};
  ${(props) => props.$hideOverflow && { overflow: "hidden" }};
  ${(props) => props.$scroll && { overflow: "scroll" }};
  ${(props) => props.$fullWidth && { width: "100%" }};
  ${(props) => props.$fullHeight && { height: "100%" }};
  ${(props) => props.$flex && { flex: props.$flex }};
  ${(props) => props.$gap && { gap: props.$gap }};
`;

export const Container = styled.div<{
  $relative?: boolean;
}>`
  ${(props) => props.$relative && { position: "relative" }}
`;
