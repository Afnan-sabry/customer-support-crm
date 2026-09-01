import { Injectable, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { DOCUMENT } from '@angular/common';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private translate = inject(TranslateService);
  private document = inject(DOCUMENT);

  private readonly STORAGE_KEY = 'crm-language';
  private readonly RTL_LANGUAGES = ['ar'];

  init(): void {
    this.translate.addLangs(['en', 'ar']);
    this.translate.setDefaultLang('en');

    const saved = localStorage.getItem(this.STORAGE_KEY);
    const lang = saved && ['en', 'ar'].includes(saved) ? saved : 'en';
    this.switchLanguage(lang as 'en' | 'ar');
  }

  switchLanguage(lang: 'en' | 'ar'): void {
    this.translate.use(lang);
    localStorage.setItem(this.STORAGE_KEY, lang);

    const dir = this.RTL_LANGUAGES.includes(lang) ? 'rtl' : 'ltr';
    this.document.documentElement.setAttribute('dir', dir);
    this.document.documentElement.setAttribute('lang', lang);
  }

  getCurrentLanguage(): string {
    return this.translate.currentLang || 'en';
  }

  isRtl(): boolean {
    return this.RTL_LANGUAGES.includes(this.getCurrentLanguage());
  }
}
