import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';

@Component({
  standalone: true,
  selector: 'app-root',
  imports: [RouterModule],
  template: `
    <div style="font-family: Arial, Helvetica, sans-serif; padding: 1rem;">
      <h1>CV Viewer App</h1>
      <router-outlet></router-outlet>
    </div>
  `
})
export class AppComponent {}
