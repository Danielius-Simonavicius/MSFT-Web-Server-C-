import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { UploadPageComponent } from './pages/upload-page/upload-page.component';
import { EditWebsiteComponent } from './pages/edit-website/edit-website.component';

const routes: Routes = [
  { path: 'upload-page', component: UploadPageComponent },
  { path: 'edit-website/:WebsiteId', component: EditWebsiteComponent },
  { path: '**', component: UploadPageComponent },
];

@NgModule({
  imports: [RouterModule.forRoot(routes, {
    useHash: true
  })],
  exports: [RouterModule]
})
export class AppRoutingModule { }
