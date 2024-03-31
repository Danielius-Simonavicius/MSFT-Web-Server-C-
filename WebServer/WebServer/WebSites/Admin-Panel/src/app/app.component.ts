import { Component, OnInit, ViewChild } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Guid } from 'guid-typescript';
import { Meta } from '@angular/platform-browser';
import { NgForm } from '@angular/forms';
interface FormData {
  name: string;
  description: string;
  guid: string
}

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})

export class AppComponent {
  @ViewChild('myForm') Form: NgForm;
  title = "Admin-Panel";
  formData: FormData = {
    name: '',
    description: '',
    guid: ''
  };
  fileToUpload: File | undefined;

  constructor() { }

  handleFileInput(event: any) {
    this.fileToUpload = event.target.files[0];
  }

  submitForm() {
    const formData = new FormData(this.Form.form);
    formData.append('name', this.formData.name);
    formData.append('description', this.formData.description);
    this.formData.name = Guid.create().toString();
    formData.append('guid', this.formData.guid);

    // Check if fileToUpload is defined before appending it
    if (this.fileToUpload) {
      formData.append('folder', this.fileToUpload);
    }
    this.formDa

    // this.http.post<any>('http://localhost:9090/uploadWebsite', formData).subscribe(
    //   response => {
    //     console.log('Upload successful:', response);
    //   },
    //   error => {
    //     console.error('Upload failed:', error);
    //   }
    // );

    // submitData(form: HTMLFormElement):void {

    //   form.submit()
    //   // formData.set("name", this.name);
    //   // formData.set("file", this.file);
    //   // fetch('http://localhost:8085/', { 
    //   //   method: 'POST',
    //   //   body: formData
    //   // }).then(res => {
    //   //   return res.json()
    //   // })
    //   // .then(data => console.log(data))
    //   // .catch(errpr => console.log('ERROR'))
    // }
  }
}
