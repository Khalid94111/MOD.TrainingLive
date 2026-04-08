import CustomStore from 'devextreme/data/custom_store';
import { LoadOptions } from 'devextreme/data';
import { PagedResultDto } from '@abp/ng.core';

/**
 * Creates a DevExtreme CustomStore that maps to ABP's paging/sorting conventions.
 *
 * DevExtreme skip/take → ABP skipCount/maxResultCount
 * DevExtreme sort      → ABP sorting string
 *
 * Usage (Angular 21 standalone component):
 *
 *   private readonly catalogService = inject(CourseCatalogService);
 *
 *   dataSource = createAbpStore<CourseCatalogDto>({
 *     loadFn: params => this.catalogService.getList(params),
 *     removeFn: key => this.catalogService.delete(key),
 *     key: 'id',
 *   });
 */

export interface AbpStoreConfig<T = any> {
  /** ABP paged list call. Must return { totalCount, items }. */
  loadFn: (params: any) => Promise<PagedResultDto<T>>;

  /** Optional: ABP create call. */
  insertFn?: (values: Partial<T>) => Promise<T>;

  /** Optional: ABP update call. */
  updateFn?: (key: string, values: Partial<T>) => Promise<T>;

  /** Optional: ABP delete call. */
  removeFn?: (key: string) => Promise<void>;

  /** Primary key field (default: 'id'). */
  key?: string;

  /** Extra params merged into every load call (search, filters). */
  extraParams?: () => Record<string, any>;
}

export function createAbpStore<T = any>(config: AbpStoreConfig<T>): CustomStore<T, string> {
  return new CustomStore<T, string>({
    key: config.key || 'id',

    load: async (loadOptions: LoadOptions<T>) => {
      const params: Record<string, any> = {
        skipCount: loadOptions.skip ?? 0,
        maxResultCount: loadOptions.take ?? 10,
      };

      // DevExtreme sort → ABP sorting string: "fieldName desc, fieldName2 asc"
      if (loadOptions.sort && Array.isArray(loadOptions.sort) && loadOptions.sort.length > 0) {
        params['sorting'] = (loadOptions.sort as any[])
          .map(s => {
            const field = typeof s === 'string' ? s : s.selector;
            const desc = typeof s === 'object' && s.desc ? ' desc' : '';
            return field + desc;
          })
          .join(', ');
      }

      // Merge caller-provided extra params (filters, search text)
      if (config.extraParams) {
        Object.assign(params, config.extraParams());
      }

      const result = await config.loadFn(params);
      return {
        data: result.items ?? [],
        totalCount: result.totalCount ?? 0,
      };
    },

    insert: config.insertFn
      ? async (values: any) => await config.insertFn!(values) as any
      : undefined,

    update: config.updateFn
      ? async (key: any, values: any) => await config.updateFn!(key, values) as any
      : undefined,

    remove: config.removeFn
      ? async (key: any) => { await config.removeFn!(key); }
      : undefined,
  });
}
