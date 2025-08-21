import { ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';
import { HTTP_INTERCEPTORS, withInterceptorsFromDi, provideHttpClient, withInterceptors } from '@angular/common/http';
import { AuthInterceptor} from './interceptors/auth.interceptor';
import { FormsModule } from '@angular/forms'; // Or wherever you import it

import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptors([AuthInterceptor]))

  ]
};
