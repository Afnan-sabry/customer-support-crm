import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, TranslateModule],
  template: `
    <h1>{{ 'app.title' | translate }}</h1>
    <router-outlet />
  `
})
export class App {}
