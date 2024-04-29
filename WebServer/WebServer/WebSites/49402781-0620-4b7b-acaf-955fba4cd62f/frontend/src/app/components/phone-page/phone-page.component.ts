import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Phone } from 'src/models/phone.model';
import { PhoneService } from 'src/services/phone.service';
@Component({
  selector: 'app-phone-page',
  templateUrl: './phone-page.component.html',
  styleUrls: ['./phone-page.component.css'],
})
export class PhonePageComponent implements OnInit {
  phone!: Phone | undefined;

  constructor(private router:Router, private activatedRoute: ActivatedRoute, private phoneService: PhoneService) {}

  ngOnInit(): void {
    this.activatedRoute.params.subscribe((params) => {
      if (params['id']) { 
        this.phoneService.getPhoneById(params['id']).subscribe((phone) => {
          this.phone = phone;
          console.log(params['id'])
        });
      }
    });
  }

  updatePhone(e: Phone): void {
    this.phoneService.updatePhone(e).subscribe(x => {
      if (x) {
        this.router.navigate(['/data/home']);
      }
    });
  }
}
