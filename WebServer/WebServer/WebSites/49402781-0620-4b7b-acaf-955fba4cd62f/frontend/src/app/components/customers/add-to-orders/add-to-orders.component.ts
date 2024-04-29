import { Component } from '@angular/core';
import { Observable } from 'rxjs';
import { Phone } from 'src/models/phone.model';
import { PhoneService } from 'src/services/phone.service';
import { ActivatedRoute, Router } from '@angular/router';
import { CustomerService } from 'src/services/customer.service';
@Component({
  selector: 'app-add-to-orders',
  templateUrl: './add-to-orders.component.html',
  styleUrls: ['./add-to-orders.component.css']
})
export class AddToOrdersComponent {
  phones$: Observable<Phone[]> = new Observable<Phone[]>(); 
  customerId: string | undefined;
  constructor(private customerService: CustomerService,private phoneService: PhoneService,private router:Router, private activatedRoute: ActivatedRoute) {}

  ngOnInit(): void {
    this.phones$ = this.phoneService.getAllPhones(); //loads all available phones to add to orders

    this.activatedRoute.params.subscribe((params) => {
        this.customerId = params['id']; //grabs cust id from route at top of page
    });
  }

  AddToOrderHistory(phoneId: string){
    this.customerService.addToOrderHistory(phoneId, this.customerId).subscribe(x => {
      if (x) {
        this.router.navigate(['/customer', this.customerId]);
      }
    });
  }
}
