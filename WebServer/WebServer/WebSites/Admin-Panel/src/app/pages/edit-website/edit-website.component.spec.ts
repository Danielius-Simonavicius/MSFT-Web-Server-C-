import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EditWebsiteComponent } from './edit-website.component';

describe('EditWebsiteComponent', () => {
  let component: EditWebsiteComponent;
  let fixture: ComponentFixture<EditWebsiteComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [EditWebsiteComponent]
    });
    fixture = TestBed.createComponent(EditWebsiteComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
