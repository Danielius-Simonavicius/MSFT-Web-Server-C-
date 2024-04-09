import { Component, OnInit, ViewChild } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { UploadWebsiteService } from 'src/services/upload-website.service';
import { UploadWebsite } from 'src/models/upload-website.model';
import { Guid } from 'guid-typescript';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css'],
})

export class AppComponent {
  formData = new FormData();
  dataFields = new UploadWebsite();
  submitted: boolean = false;
  constructor(private uploadWebsiteService: UploadWebsiteService) {}
  
  onFolderSelected(event: any) {
    const files = event.target.files;
    if (files.length > 0) {
      const file =  event.target.files[0];
      this.formData.append("WebsiteFile", file);
    }
  }

  onSubmit() {
   
    this.formData.append("AllowedHosts", this.dataFields.allowedHosts);
    this.formData.append("Path", this.dataFields.path);
    this.formData.append("DefaultPage", this.dataFields.defaultPage);
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
