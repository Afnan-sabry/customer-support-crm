import { Pipe, PipeTransform } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

@Pipe({ name: 'localizedName', pure: false })
export class LocalizedNamePipe implements PipeTransform {
  constructor(private translate: TranslateService) {}

  transform(item: { name: string; nameAr: string } | null | undefined): string {
    if (!item) return '';
    return this.translate.currentLang === 'ar' ? item.nameAr : item.name;
  }
}
