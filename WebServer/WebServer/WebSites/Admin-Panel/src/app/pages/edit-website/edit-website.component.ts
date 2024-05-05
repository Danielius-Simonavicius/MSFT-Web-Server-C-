import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Guid } from 'guid-typescript';
import { Website } from 'src/models/website-list.model';
import { WebsiteService } from 'src/services/website.service';

@Component({
  selector: 'app-edit-website',
  templateUrl: './edit-website.component.html',
  styleUrls: ['./edit-website.component.css'],
})
export class EditWebsiteComponent implements OnInit {
  website!: Website | undefined;

  constructor(
    private websiteService: WebsiteService,
    private router: Router,
    private activatedRoute: ActivatedRoute
  ) {}
  ngOnInit(): void {
    this.activatedRoute.params.subscribe((params) => {
      this.loadData(params['WebsiteId']);
    });
  }

  loadData(id: string) {
    this.websiteService.getWebsite(id).subscribe((x) => {
      this.website = x;
    });
  }

  deleteWebsite(WebsiteId: string) {
    this.websiteService.deleteWebsite(WebsiteId).subscribe({
      next: () => {
        // Optionally display a message or handle the UI update
        console.log('Website deleted successfully');
        this.router.navigate(['/upload-page']);
      },
      error: (error: any) => {
        console.error('Error deleting the website:', error);
      },
    });
  }

  updateWebsite(updatedWebsite: Website) {
    this.websiteService.updateWebsite(updatedWebsite).subscribe();
    this.loadData;
    this.router.navigate(['/upload-page']);
  }
}
