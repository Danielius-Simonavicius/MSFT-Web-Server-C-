import { Component, OnInit } from '@angular/core';
import { Customer } from 'src/models/customer.model';
import { CustomerService } from 'src/services/customer.service';
import { Router } from '@angular/router';
@Component({
  selector: 'app-add-customer',
  templateUrl: './add-customer.component.html',
  styleUrls: ['./add-customer.component.css']
})
export class AddCustomerComponent implements OnInit {
  newCustomer: Customer = {
    _id: '',
    title: '',
    firstName: '',
    surname: '',
    mobile: '',
    emailAddress: '',
    homeAddress: {
      addressLine1: '',
      town: '',
      countyCity: '',
      eircode: ''
    },
    purchaseHistory: []
  };



  constructor(private customerService: CustomerService, private router: Router,) {}

  ngOnInit(): void {}

  addCustomer(model: Customer) {
    this.customerService.addNewCustomer(model)
      .subscribe(
        (response) => {
          // Handle success - for example, display a success message or navigate to another page
          console.log('New customer added successfully:', response);
          this.router.navigate(['/customers']); // Navigate to '/success' route
        },
        (error) => {
          // Handle error - for example, display an error message to the user
          console.error('Error adding new customer:', error);
        }
      );
  }
}