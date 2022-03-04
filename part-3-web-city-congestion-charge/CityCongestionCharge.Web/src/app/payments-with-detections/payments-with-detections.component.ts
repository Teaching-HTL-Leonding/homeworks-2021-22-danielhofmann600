import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { PaymentWithDetectionDto, PaymentType } from '../model';

// Todo: Complete the component logic

@Component({
  selector: 'app-payments-with-detections',
  templateUrl: './payments-with-detections.component.html',
  styleUrls: ['./payments-with-detections.component.css']
})
export class PaymentsWithDetectionsComponent implements OnInit {

  payments: PaymentWithDetectionDto[] = [];

  constructor(private http: HttpClient) {
  }

  ngOnInit(): void {
    const p = this.http.get<PaymentWithDetectionDto[]>("https://localhost:7182/api/Payments/with-detections");
    p.subscribe(v => this.payments = v);
  }

  getPaymentTypeDescription(type: PaymentType): string {
    switch (type) {
      case PaymentType.Cash:
        return "Cash";
      case PaymentType.BankTransfer:
        return "Bank Transfer";
      case PaymentType.CreditCard:
        return "Creditcard";
      case PaymentType.DebitCard:
        return "Debitcard";
      default:
        return "unknown";
    }
  }
}
