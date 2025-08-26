import { ComponentFixture, TestBed } from '@angular/core/testing';

import { StockAdderComponent } from './stock-adder.component';

describe('StockAdderComponent', () => {
  let component: StockAdderComponent;
  let fixture: ComponentFixture<StockAdderComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StockAdderComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(StockAdderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
