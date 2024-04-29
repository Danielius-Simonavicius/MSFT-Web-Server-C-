import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { PhonePageComponent } from './components/phone-page/phone-page.component';
import { HomeComponent } from './components/home/home.component';
import { CustomersComponent } from './components/customers/customers.component';
import { CustomerComponent } from './components/customers/customer/customer.component';
import { AboutPageComponent } from './components/about-page/about-page.component';
import { AddCustomerComponent } from './components/customers/add-customer/add-customer.component';
import { AddToOrdersComponent } from './components/customers/add-to-orders/add-to-orders.component';
const routes: Routes = [
  { path: 'phone-page/:id', component: PhonePageComponent },
  { path: 'customers', component: CustomersComponent },
  { path: 'customer/:id', component: CustomerComponent },
  { path: 'addCustomer', component: AddCustomerComponent },
  { path: 'aboutPage', component: AboutPageComponent }, 
  { path: 'addOrder/:id', component: AddToOrdersComponent },  
  { path: '**', component: HomeComponent },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule {}
