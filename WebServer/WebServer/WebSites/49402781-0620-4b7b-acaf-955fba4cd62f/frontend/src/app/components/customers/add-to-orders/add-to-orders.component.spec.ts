import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AddToOrdersComponent } from './add-to-orders.component';

describe('AddToOrdersComponent', () => {
  let component: AddToOrdersComponent;
  let fixture: ComponentFixture<AddToOrdersComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [AddToOrdersComponent]
    });
    fixture = TestBed.createComponent(AddToOrdersComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
