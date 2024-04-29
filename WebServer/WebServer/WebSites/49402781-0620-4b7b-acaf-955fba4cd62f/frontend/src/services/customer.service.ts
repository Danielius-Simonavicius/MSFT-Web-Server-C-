import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { environment } from './enviorment';
import { map } from 'rxjs/operators';
import { Customer } from 'src/models/customer.model';

@Injectable({
  providedIn: 'root',
})
export class CustomerService {
  constructor(private http: HttpClient) {}

  // Method to retrieve all customers
  getAllCustomers(): Observable<Customer[]> {
    return this.http.get<Customer[]>(`${environment.apiUrl}/api/customers`);
  }

  // Method to add a new purchase to a customer's order history
  addToOrderHistory(
    customerId: string,
    phoneId: string | undefined
  ): Observable<Customer | undefined> {
    return this.http.post<Customer>(
      `${environment.apiUrl}/api/customers/${customerId}/order`,
      { phoneId: phoneId } // Structure the object properly
    );
  }

  // Method to retrieve a customer by ID
  getCustomerById(customerId: string): Observable<Customer | undefined> {
    return this.getAllCustomers().pipe(
      // Use pipe operator
      map((customer) =>
        customer.find((customer) => customer._id === customerId)
      )
    );
  }

  // Method to update a customer
  updateCustomer(model: Customer): Observable<Customer> {
    return this.http.put<Customer>(
      `${environment.apiUrl}/api/Customer/${model._id}`,
      model
    );
  }

  // Method to delete a customer
  deleteCustomer(customerId: string): Observable<Customer | undefined> {
    return this.http.delete<Customer>(
      `${environment.apiUrl}/api/delete/customer/${customerId}`
    );
  }

  // Method to add a new customer
  addNewCustomer(customer: Customer): Observable<Customer | undefined> {
    return this.http.post<Customer>(
      `${environment.apiUrl}/api/create/customer`,
      customer
    );
  }

  getRandomCustomer(): Observable<Customer>{
    return this.http.get<Customer>(`${environment.apiUrl}/api/random/customer`);
  }
}
