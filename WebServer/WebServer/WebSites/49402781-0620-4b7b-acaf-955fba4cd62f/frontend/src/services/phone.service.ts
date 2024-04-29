import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { environment } from './enviorment';
import { map } from 'rxjs/operators'; // Import the map operator
import { Phone } from 'src/models/phone.model';

@Injectable({
  providedIn: 'root'
})
export class PhoneService {
  constructor(private http: HttpClient) { }


  getAllPhones(): Observable<Phone[]> {
    return this.http.get<Phone[]>(`${environment.apiUrl}/api/phones`);
  }


  addToOrderHistory(phoneId: string, customerId: string){

  }

  getPhoneById(phoneId: string): Observable<Phone | undefined> {
    return this.getAllPhones().pipe( 
      map(phones => phones.find(phone => phone.id === phoneId)) 
    );
  }

  updatePhone(model: Phone): Observable<Phone> {
    return this.http.put<Phone>(`${environment.apiUrl}/api/phones/${model.id}`, model);
  }

}
