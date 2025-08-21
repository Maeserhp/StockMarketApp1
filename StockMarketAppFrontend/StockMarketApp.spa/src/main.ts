import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { MainPageComponent } from './app/pages/main-page/main-page.component';
import { AuthInterceptor  } from './app/interceptors/auth.interceptor';
import { LoginComponent } from './app/pages/login-page/login/login.component';
import { provideRouter } from '@angular/router';
import { routes } from './app/app.routes';


bootstrapApplication(AppComponent, {
  providers: [
    provideHttpClient(), 
    provideRouter(routes),
    provideHttpClient(withInterceptors([AuthInterceptor]))
  ]
})
  .catch((err) => console.error(err));


