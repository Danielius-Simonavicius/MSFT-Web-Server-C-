import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/services/environment';
import { UploadWebsite } from '../models/upload-website.model';
@Injectable({
  providedIn: 'root'
})
export class UploadWebsiteService {

  constructor(private http: HttpClient) { }

  uploadWebsite(model: FormData): Observable<any> {
    return this.http.post<any>(`${environment.apiUrl}upload`, model);
  }
}
