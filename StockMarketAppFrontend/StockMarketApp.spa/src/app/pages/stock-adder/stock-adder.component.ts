import { Component, EventEmitter, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StockService } from '../../services/stock-service/stock.service';
import { StockSearchResponse } from '../../models/StockSearchResponse';

@Component({
  selector: 'app-stock-adder',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './stock-adder.component.html',
  styleUrls: [
    '../../styles/shared.css',
    './stock-adder.component.css'
  ]
})
export class StockAdderComponent {
  @Output() itemAdded  = new EventEmitter<string>();

  currentStep: 'initial' | 'text-input' | 'option-selection' | 'complete' | 'failed' = 'initial';
  enteredText: string = '';
  options: string[] = [];
  stocksResultsTitle: string = 'Please select one copmany stock to start tracking';
  chosenOption: string | null = null;

  constructor(private stockService: StockService) { }


  startProcess() {
    this.currentStep = 'text-input';
  }

  searchText() {
    // Logic to handle the text submission

    this.stockService.searchStocks(this.enteredText).subscribe({
      next: (result: StockSearchResponse) => {
        
        if (result.result.length > 0){
          result.result.forEach(stockSymbol => {
            this.options.push(`${stockSymbol.symbol} (${stockSymbol.description})`)
          });
        } else {
          this.stocksResultsTitle = "Sorry, no stocks were found that were similar to your search."
        }
        

        this.currentStep = 'option-selection';
        
        // The items list is populated here
      },
      error: (err) => {
        console.error('Failed to search stocks:', err);
        // Handle error (e.g., show a user message)
      }
    });

  }

  selectedOption(option: string) {
      this.chosenOption = option; // ensures only one is selected at a time

  }

  submitOption() {

    let stockSymbol = this.chosenOption?.split(' ')[0].trim();
    if (!stockSymbol) {
      return;
    }

    this.stockService.addNewStock(stockSymbol).subscribe({
      next: result => {
        this.currentStep = 'complete';
        console.log(`add new stock response ${result}`);
        this.itemAdded.emit(stockSymbol);
      },
      error: (err) => {
        this.currentStep = 'failed';
        console.error('Failed to save new stock:', err);
      }
    });
  }

  reset() {
    this.currentStep = 'initial';
    this.options = [];
    this.chosenOption = null;
    this.enteredText = '';
  }

}
