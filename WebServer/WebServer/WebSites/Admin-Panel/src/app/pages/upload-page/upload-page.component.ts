import { Component, OnInit } from '@angular/core';
import { WebsiteService } from 'src/services/website.service';
import { UploadWebsite } from 'src/models/upload-website.model';
import { Website } from 'src/models/website-list.model';
import { Observable, of } from 'rxjs';

@Component({
  selector: 'app-upload-page',
  templateUrl: './upload-page.component.html',
  styleUrls: ['./upload-page.component.css']
})
export class UploadPageComponent implements OnInit{
  formData = new FormData();
  
  websites$: Observable<Website[]> | undefined; 
  dataFields = new UploadWebsite();
  submitted: boolean = false;

  constructor(private websiteService: WebsiteService) {}


  ngOnInit(): void {
    this.loadList();
  }

  loadList(){
    this.websiteService.getAllWebsites().subscribe(
      (websites) => {
        console.log('Websites:', websites);
        this.websites$ = of(websites); // Use of() to convert array to observable
      },
      (error) => {
        console.error('Error fetching websites:', error);
      }
    );
  }
  
  onFolderSelected(event: any) {
    const files = event.target.files;
    if (files.length > 0) {
      const file =  event.target.files[0];
      this.formData.append("WebsiteFile", file);
    }
  }

  onSubmit() {
    this.formData.append("WebsiteName", this.dataFields.WebsiteName);
    this.formData.append("AllowedHosts", this.dataFields.allowedHosts);
    this.formData.append("Path", this.dataFields.path);
    this.formData.append("DefaultPage", this.dataFields.defaultPage);
    this.websiteService.uploadWebsite(this.formData).subscribe(
      response => {
        console.log('Upload successful:', response);
        this.submitted = true;
        window.location.reload();
      },
      error => {
        console.error('Error uploading:', error);
        window.location.reload();
      }
    );
    this.submitted = true;
  }
}
