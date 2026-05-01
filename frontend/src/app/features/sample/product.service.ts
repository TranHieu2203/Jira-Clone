import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedList } from '@shared/models/api-response';
import { APP_CONFIG } from '@core/config/app-config';

export interface Product {
  id: string;
  name: string;
  sku: string;
  price: number;
  description?: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

export interface CreateProductRequest {
  name: string;
  sku: string;
  price: number;
  description?: string | null;
}

export interface UpdateProductRequest {
  name: string;
  price: number;
  description?: string | null;
  isActive: boolean;
}

export interface ProductFilter {
  pageIndex?: number;
  pageSize?: number;
  search?: string;
  isActive?: boolean;
}

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly http = inject(HttpClient);
  private readonly cfg = inject(APP_CONFIG);
  private readonly base = `${this.cfg.apiBaseUrl}/v1/products`;

  search(f: ProductFilter): Observable<PagedList<Product>> {
    let params = new HttpParams();
    if (f.pageIndex) params = params.set('pageIndex', f.pageIndex);
    if (f.pageSize) params = params.set('pageSize', f.pageSize);
    if (f.search) params = params.set('search', f.search);
    if (f.isActive !== undefined) params = params.set('isActive', f.isActive);
    return this.http.get<PagedList<Product>>(this.base, { params });
  }

  create(req: CreateProductRequest): Observable<Product> {
    return this.http.post<Product>(this.base, req);
  }

  update(id: string, req: UpdateProductRequest): Observable<Product> {
    return this.http.put<Product>(`${this.base}/${id}`, req);
  }

  delete(id: string): Observable<unknown> {
    return this.http.delete(`${this.base}/${id}`);
  }
}
