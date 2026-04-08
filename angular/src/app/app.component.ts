import { Component } from '@angular/core';
import { GdprCookieConsentComponent } from '@volo/abp.ng.gdpr/config';
import { DynamicLayoutComponent, SessionStateService } from '@abp/ng.core';
import { LoaderBarComponent } from '@abp/ng.theme.shared';
 import config from 'devextreme/core/config';

@Component({
  selector: 'app-root',
  template: `
    <abp-loader-bar />
    <abp-dynamic-layout />
    <abp-gdpr-cookie-consent />
  `,
  imports: [LoaderBarComponent, DynamicLayoutComponent, GdprCookieConsentComponent],
})
export class AppComponent {
 private rtlLanguages = ['ar', 'ar-SA', 'ar-EG', 'he', 'fa', 'ur'];

  constructor(private sessionState: SessionStateService) {
    this.listenToLanguageChanges();
  }
 
private listenToLanguageChanges() {
    this.sessionState.getLanguage$().subscribe(lang => {
      const isRtl = this.rtlLanguages.some(r => lang?.startsWith(r));
      
      // Set DevExtreme RTL globally
      config({ rtlEnabled: isRtl });
      
      // Set document direction
      document.documentElement.dir = isRtl ? 'rtl' : 'ltr';
      document.body.dir = isRtl ? 'rtl' : 'ltr';
    });
  }
  
}

