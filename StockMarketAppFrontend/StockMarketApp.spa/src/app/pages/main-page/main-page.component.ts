import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { StockService } from '../../services/stock-service/stock.service';
import { StockHistory } from '../../models/stockHistory';
import { Quote } from '../../models/Quote';
import { StockAdderComponent } from '../stock-adder/stock-adder.component';

// import { HttpClientModule } from '@angular/common/http';


@Component({
  selector: 'app-main-page',
  standalone: true,
  imports: [CommonModule, FormsModule, StockAdderComponent],
  templateUrl: './main-page.component.html',
  styleUrls: [
    '../../styles/shared.css',
    './main-page.component.css'
  ]
})
export class MainPageComponent implements OnInit {
  items: string[] = [];
  filteredItems: string[] = [];
  searchTerm: string = '';

  selectedItem: string = 'Test';
  stockHistory?: StockHistory;
  dailyQuotes?: Quote[];
  showCurrentQuote: boolean = false;
  selectedCurrentQuote?: Quote;
  Date = Date; // assign the global Date object to a property

  constructor(private stockService: StockService) { }

  ngOnInit(): void {
    this.stockService.getStocks().subscribe({
      next: (stocks: string[]) => {
        this.items = stocks;
      },
      error: (err) => {
        console.error('Failed to load stocks:', err);
      }
    });
  }
  

  onSearchChange(): void {
    const term = this.searchTerm.toLowerCase();
    this.filteredItems = this.items.filter(item =>
      item.toLowerCase().includes(term)
    );
  }

  onSearchFocus() {
    if(this.searchTerm == ''){
      this.filteredItems = this.items;
    }
  }

  onSearchBlur() {
    // this.filteredItems = [];
    // this.searchTerm = '';

    // Add a small delay so the (click) event on the dropdown item can fire first
    setTimeout(() => {
      this.filteredItems = []; // This action will remove the dropdown
      this.searchTerm = '';
      console.log('Dropdown menu cleared by blur event.');
    }, 150); // Delay of 150ms

  }

  onItemSelected(item: string): void {
    // Display a notification
    this.showCurrentQuote = false;

    this.stockService.getSelectedStockHistory(item).subscribe({
      next: (history: StockHistory) => {
        this.stockHistory = history;

        if(this.stockHistory.quoteHistory.length > 0){
          this.dailyQuotes = this.stockHistory?.quoteHistory
        } else {
          this.dailyQuotes = [];
        }
        this.selectedItem = item
        this.filteredItems = [];
      },
      error: (err) => {
        console.error('Failed to load history:', err);
      }
    });
  }

  onCheckPrice() {
    this.stockService.getCurrentQuote(this.selectedItem).subscribe({
      next: (quote: Quote) => {
        this.selectedCurrentQuote = quote;
        this.showCurrentQuote = true;
      },
      error: (err) => {
        console.error('Failed to load history:', err);
      }
    });
  }

  onNewStockAdded(newItem: string) {
    this.items.push(newItem);
  }

  onDeleteStock(): void {
    const isConfirmed = confirm('Warning: If you delete this stock, it will no longer be tracked and you will lose access to all its historical data. Do you wish to continue?');
      console.log('Stock deletion confirmed. Proceeding with the deletion process...');

    if (isConfirmed) {

      this.stockService.deleteStock(this.selectedItem).subscribe({
        next: () => {
          console.log('Stock deleted successfully.');
          this.items = this.items.filter(item => item !== this.selectedItem);
          this.selectedItem = "";
          this.stockHistory = undefined;
          this.dailyQuotes = [];
          this.showCurrentQuote = false;
          this.selectedCurrentQuote = undefined;
        },
        error: (err) => {
          console.error('Failed to delete stock:', err);
        }
      });

    } else {
      console.log('Stock deletion cancelled.');
    }
  }
}
