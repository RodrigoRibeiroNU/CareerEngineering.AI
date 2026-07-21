import { Component, inject } from '@angular/core';
import { ToastService } from '../../services/toast';

@Component({
  selector: 'app-toast',
  standalone: true,
  templateUrl: './toast.html',
})
export class ToastComponent {
  protected readonly toast = inject(ToastService);
}
