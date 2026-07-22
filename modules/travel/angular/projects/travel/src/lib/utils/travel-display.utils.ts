import { RequestStatus } from '../models/travel-types';
import { TicketClass } from '../services/travel-request.service';

export function getStatusBadgeClass(status: RequestStatus): string {
  switch (status) {
    case RequestStatus.Completed:
      return 'completed';
    case RequestStatus.Rejected:
    case RequestStatus.Cancelled:
      return 'rejected';
    case RequestStatus.Returned:
      return 'returned';
    case RequestStatus.Draft:
      return 'draft';
    case RequestStatus.AtTravelOffice:
      return 'travel-office';
    case RequestStatus.TicketsBooked:
      return 'tickets-booked';
    case RequestStatus.CalculatingAllowances:
      return 'calculating';
    default:
      return 'pending';
  }
}

export function getTicketClassLabel(ticketClass: TicketClass | number, localize: (key: string) => string): string {
  return ticketClass === TicketClass.Business
    ? localize('Travel::TicketClass.Business')
    : localize('Travel::TicketClass.Economy');
}

export function getMimeType(extension: string): string {
  const ext = (extension || '').toLowerCase().replace('.', '');
  const types: Record<string, string> = {
    pdf: 'application/pdf',
    png: 'image/png',
    jpg: 'image/jpeg',
    jpeg: 'image/jpeg',
    gif: 'image/gif',
    webp: 'image/webp',
    doc: 'application/msword',
    docx: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
    xls: 'application/vnd.ms-excel',
    xlsx: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    txt: 'text/plain',
    zip: 'application/zip',
  };
  return types[ext] || 'application/octet-stream';
}

export function travelLocalize(key: string): string {
  return key.startsWith('::') || key.includes('::') ? key : `Travel::${key}`;
}
