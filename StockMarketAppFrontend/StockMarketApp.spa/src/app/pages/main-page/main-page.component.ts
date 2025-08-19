import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms'; // <-- Import FormsModule
import { CommonModule } from '@angular/common';
import { StockService } from '../../services/stock.service';
// import { HttpClientModule } from '@angular/common/http';


@Component({
  selector: 'app-main-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './main-page.component.html',
  styleUrl: './main-page.component.css'
})
export class MainPageComponent implements OnInit {
   items: string[] = [
    'Apple',
    'Banana',
    'Cherry',
    'Date',
    'Grape',
    'Lemon',
    'Mango',
    'Orange',
    'Peach',
    'Pear'
  ];
  filteredItems: string[] = [];

  // The search term from the input field
  searchTerm: string = '';
  queries = [
    // { symbol: 'AAPL', name: 'Apple Inc.' },
    // { symbol: 'MSFT', name: 'Microsoft Corp.' }
  ];

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

  onItemSelected(item: string): void {
    // Display a notification
    alert(`You selected: ${item}`);

    // Optional: Clear the search bar after selection
    this.searchTerm = '';
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
