import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Reusable confirmation modal (gtms style) — replaces the native browser confirm().
 * Usage:
 *   <gtms-confirm-dialog
 *     [isOpen]="confirmOpen()"
 *     [title]="'حذف الجهة'"
 *     [message]="'هل أنت متأكد من حذف هذه الجهة؟'"
 *     [confirmText]="'حذف'"
 *     [danger]="true"
 *     (confirmed)="onConfirmed()"
 *     (cancelled)="onCancelled()">
 *   </gtms-confirm-dialog>
 */
@Component({
  standalone: true,
  selector: 'gtms-confirm-dialog',
  templateUrl: './confirm-dialog.component.html',
  styleUrl: './confirm-dialog.component.scss',
  imports: [CommonModule],
})
export class ConfirmDialogComponent {
  isOpen = input.required<boolean>();
  title = input<string>('تأكيد');
  message = input<string>('هل أنت متأكد؟');
  detail = input<string>('');          // optional second line (e.g. the item name)
  confirmText = input<string>('تأكيد');
  cancelText = input<string>('إلغاء');
  danger = input<boolean>(false);      // red confirm button + warning icon
  icon = input<string>('');            // override the header icon emoji

  confirmed = output<void>();
  cancelled = output<void>();

  get headerIcon(): string {
    return this.icon() || (this.danger() ? '🗑️' : '❓');
  }

  onConfirm(): void { this.confirmed.emit(); }
  onCancel(): void { this.cancelled.emit(); }

  onBackdropClick(event: Event): void {
    if (event.target === event.currentTarget) this.onCancel();
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') this.onCancel();
  }
}
