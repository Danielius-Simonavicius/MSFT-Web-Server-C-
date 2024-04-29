import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PhonePageComponent } from './phone-page.component';

describe('PhonePageComponent', () => {
  let component: PhonePageComponent;
  let fixture: ComponentFixture<PhonePageComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [PhonePageComponent]
    });
    fixture = TestBed.createComponent(PhonePageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
