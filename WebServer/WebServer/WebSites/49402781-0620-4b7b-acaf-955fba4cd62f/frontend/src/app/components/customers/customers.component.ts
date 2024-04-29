import { Component, OnInit } from '@angular/core';
import { Customer } from 'src/models/customer.model';
import { Observable } from 'rxjs';
import { CustomerService } from 'src/services/customer.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-customers',
  templateUrl: './customers.component.html',
  styleUrls: ['./customers.component.css']
})

export class CustomersComponent implements OnInit{


  customers$: Observable<Customer[]> = new Observable<Customer[]>(); 
  randomCustomer: Customer | undefined;
  constructor(private router: Router,private customerService: CustomerService) {}

  ngOnInit(): void {
    this.customers$ = this.customerService.getAllCustomers();
  }

  deleteCustomer(customerId:string){
    this.customerService.deleteCustomer(customerId).subscribe((x) => {
      if (x) {
        this.router.navigate(['/customers']);
      }
    });
  }

  getRandomCustomer(){
    this.customerService.getRandomCustomer().subscribe(
      (x: Customer) => {
        this.randomCustomer = x;
      });
  }
}