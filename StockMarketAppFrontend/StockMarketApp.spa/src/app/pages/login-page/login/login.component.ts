import { Component } from '@angular/core';
import { AuthService } from '../../../services/auth-service/auth.service';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';


@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  username = 'testuser'; // Example
  password = 'password'; // Example
  error: string | null = null;

  constructor(private authService: AuthService, private router: Router) {}

  login() {
    this.error = null;

    this.authService.login(this.username, this.password)
      .subscribe(
        response => {
          this.authService.storeToken(response.token);
          console.log('Login successful! Token stored.');
          this.router.navigate(['/main']);
        },
        error => {
          console.error('Login failed:', error);
          this.error = 'Login failed. Please check your credentials.';

        }
      );
  }
}
