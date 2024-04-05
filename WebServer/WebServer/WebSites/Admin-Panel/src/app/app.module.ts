import { NgModule } from '@angular/core';
import { UploadWebsiteService } from 'src/services/upload-website.service';
import { BrowserModule } from '@angular/platform-browser';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { FormsModule } from '@angular/forms';

@NgModule({
  declarations: [
    AppComponent
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    FormsModule
  ],
  providers: [UploadWebsiteService],
  bootstrap: [AppComponent]
})
export class AppModule { }
