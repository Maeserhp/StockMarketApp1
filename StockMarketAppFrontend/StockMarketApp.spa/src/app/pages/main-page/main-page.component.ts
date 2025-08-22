import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { StockService } from '../../services/stock-service/stock.service';
// import { HttpClientModule } from '@angular/common/http';


@Component({
  selector: 'app-main-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './main-page.component.html',
  styleUrls: [
    '../../styles/shared.css',
    './main-page.component.css'
  ]
})
export class MainPageComponent implements OnInit {
  items: string[] = [];
  selectedItem: string = 'Test';
  filteredItems: string[] = [];
  searchTerm: string = '';
  queries: string[] = [];

  constructor(private stockService: StockService) { }

  ngOnInit(): void {
    this.stockService.getStocks().subscribe({
      next: (stocks: string[]) => {
        this.items = stocks;
        // The items list is populated here
      },
      error: (err) => {
        console.error('Failed to load stocks:', err);
        // Handle error (e.g., show a user message)
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
    this.queries = ["1", "2", "3"]
    this.selectedItem = item
    
    // Optional: Clear the search bar after selection
    //this.searchTerm = '';
    this.filteredItems = [];
  }


  onAddStock() {
    console.log('Add stock clicked');
    // TODO: show add-stock dialog
  }

  onRunUpdate() {
    console.log('Run daily update clicked');
    // TODO: call backend API to trigger update
  }
}
