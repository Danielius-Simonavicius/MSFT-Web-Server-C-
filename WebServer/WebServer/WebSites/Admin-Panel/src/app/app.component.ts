import { Component } from '@angular/core';
import { HttpClient } from '@angular/common/http'
import { Meta } from '@angular/platform-browser';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {
  title = 'Admin-Panel';
  name: string = "";
  file: any;

  constructor() {
  }

  getName(name: string) {
    this.name = name;
  }
  getFolder(event: any) {
    this.file = event.target.files[0];
    console.log('file', this.file)
  }

  submitData() {
    let formData = new FormData();
    formData.set("name", this.name);
    formData.set("file", this.file);
    fetch('http://localhost:9090/uploadWebsite', { 
      method: 'POST',
      body: formData
    }).then(res => {
      return res.json()
    })
    .then(data => console.log(data))
    .catch(errpr => console.log('ERROR'))
    // this.http.post('http://localhost:9090/uploadWebsite', formData).subscribe(
    //   (response) => {

    //   }
    // )
  }
}
