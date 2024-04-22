import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { UploadPageComponent } from './pages/upload-page/upload-page.component';

const routes: Routes = [
  { path: 'upload-page', component: UploadPageComponent },
  { path: '**', component: UploadPageComponent },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
