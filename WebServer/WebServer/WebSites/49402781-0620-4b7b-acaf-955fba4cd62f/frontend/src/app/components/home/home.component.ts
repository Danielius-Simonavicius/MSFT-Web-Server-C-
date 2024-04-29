import { Component, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { Phone } from 'src/models/phone.model';
import { PhoneService } from 'src/services/phone.service';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {

  phones$: Observable<Phone[]> = new Observable<Phone[]>(); 

  constructor(private phoneService: PhoneService) {}

  ngOnInit(): void {
    this.phones$ = this.phoneService.getAllPhones();
  }
}
