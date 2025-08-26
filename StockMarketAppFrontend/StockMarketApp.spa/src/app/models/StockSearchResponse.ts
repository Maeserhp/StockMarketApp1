import { StockSymbol } from "./StockSymbol";

export interface StockSearchResponse {
  coung: number;
  result: StockSymbol[];
}
