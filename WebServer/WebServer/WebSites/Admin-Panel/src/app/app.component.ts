import { Component, OnInit, ViewChild } from '@angular/core';
import { UploadWebsite } from 'src/models/upload-website.model';
import { UploadWebsiteService } from 'src/services/upload-website.service';
import {MatProgressBarModule} from '@angular/material/progress-bar';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css'],
})

export class AppComponent {
  formData = new UploadWebsite();
  submitted: boolean = false;
  constructor(private uploadWebsiteService: UploadWebsiteService) {}


  onSubmit() {
    this.uploadWebsiteService.uploadWebsite(this.formData).subscribe(
      response => {
        console.log('Upload successful:', response);
        this.submitted = true;
      },
      error => {
        console.error('Error uploading:', error);
      }
    );
    this.submitted = true;
  }
}
