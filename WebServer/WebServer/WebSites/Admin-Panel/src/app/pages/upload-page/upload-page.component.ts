import { Component, OnInit } from '@angular/core';
import { UploadWebsiteService } from 'src/services/upload-website.service';
import { UploadWebsite } from 'src/models/upload-website.model';

@Component({
  selector: 'app-upload-page',
  templateUrl: './upload-page.component.html',
  styleUrls: ['./upload-page.component.css']
})
export class UploadPageComponent implements OnInit{
  formData = new FormData();
  dataFields = new UploadWebsite();
  submitted: boolean = false;
  constructor(private uploadWebsiteService: UploadWebsiteService) {}


  ngOnInit(): void {
  }
  
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
