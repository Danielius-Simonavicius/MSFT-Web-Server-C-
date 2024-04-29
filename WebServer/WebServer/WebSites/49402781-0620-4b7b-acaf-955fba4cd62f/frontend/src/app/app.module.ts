import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HttpClientModule } from '@angular/common/http';
import { AppComponent } from './app.component';
import { PhoneService } from 'src/services/phone.service';
import { FormsModule } from '@angular/forms'; 
import { AppRoutingModule } from './app-routing.module';
import { PhonePageComponent } from './components/phone-page/phone-page.component';
import { HomeComponent } from './components/home/home.component';
import { NavbarComponent } from './navbar/navbar.component';
import { CustomersComponent } from './components/customers/customers.component';
import { CustomerComponent } from './components/customers/customer/customer.component';
import { AboutPageComponent } from './components/about-page/about-page.component';
import { AddCustomerComponent } from './components/customers/add-customer/add-customer.component';
import { AddToOrdersComponent } from './components/customers/add-to-orders/add-to-orders.component';

@NgModule({
  declarations: [
    AppComponent,
    PhonePageComponent,
    HomeComponent,
    NavbarComponent,
    CustomersComponent,
    CustomerComponent,
    AboutPageComponent,
    AddCustomerComponent,
    AddToOrdersComponent,
  ],
  imports: [
    BrowserModule,
    HttpClientModule,
    AppRoutingModule,
    FormsModule
  ],
  providers: [PhoneService],
  bootstrap: [AppComponent]
})
export class AppModule { }
