import { NgModule } from '@angular/core';
import { WebsiteService } from 'src/services/website.service';
import { BrowserModule } from '@angular/platform-browser';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { UploadPageComponent } from './pages/upload-page/upload-page.component';

@NgModule({
  declarations: [
    AppComponent,
    UploadPageComponent
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    FormsModule,
    HttpClientModule,
    BrowserAnimationsModule,
  ],
  providers: [WebsiteService],
  bootstrap: [AppComponent]
})
export class AppModule { }
