import { Component, OnInit } from '@angular/core';
import { Customer } from 'src/models/customer.model';
import { Router } from '@angular/router';
import { ActivatedRoute } from '@angular/router';
import { CustomerService } from 'src/services/customer.service';

@Component({
  selector: 'app-customer',
  templateUrl: './customer.component.html',
  styleUrls: ['./customer.component.css'],
})
export class CustomerComponent implements OnInit {
  customer!: Customer | undefined;

  constructor(
    private router: Router,
    private activatedRoute: ActivatedRoute,
    private customerService: CustomerService
  ) {}

  ngOnInit(): void {
    this.activatedRoute.params.subscribe((params) => {
      if (params['id']) {
        this.customerService.getCustomerById(params['id']).subscribe((cust) => {
          this.customer = cust;
        });
      }
    });
  }

  updateCustomer(e: Customer) {
    this.customerService.updateCustomer(e).subscribe((x) => {
      if (x) {
        this.router.navigate(['/customers']);
      }
    });
  }

  addOrder(){
    
  }
}
