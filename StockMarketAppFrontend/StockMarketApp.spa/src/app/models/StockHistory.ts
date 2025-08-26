import { Quote } from "./Quote";

export interface StockHistory {
  id: string;
  createdOn: Date;
  lastUpdated: Date;
  quoteHistory: Quote[];
}
