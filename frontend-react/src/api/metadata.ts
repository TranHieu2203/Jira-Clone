import { api } from '@/lib/api';

export enum MetadataType {
  Text = 1,
  Number = 2,
  Date = 3,
  Currency = 4,
  Textarea = 5,
}

export interface MetadataDto {
  id: string;
  value: string;
  label: string;
  type: MetadataType;
  fieldGroup: string | null;
  description: string | null;
  validationJson: string | null;
  createdAt: string;
}

export interface CreateMetadataRequest {
  value: string;
  label: string;
  type: MetadataType;
  description?: string | null;
  validationJson?: string | null;
}

export interface UpdateMetadataRequest {
  label: string;
  type: MetadataType;
  description?: string | null;
  validationJson?: string | null;
}

const base = '/v1/form-management/metadata';

export const metadataApi = {
  search: (keyword?: string, group?: string) => {
    const params = new URLSearchParams();
    if (keyword?.trim()) params.set('keyword', keyword.trim());
    if (group?.trim()) params.set('group', group.trim());
    const q = params.toString();
    return api.get<MetadataDto[]>(`${base}${q ? '?' + q : ''}`);
  },
  getById: (id: string) => api.get<MetadataDto>(`${base}/${id}`),
  create: (body: CreateMetadataRequest) => api.post<MetadataDto>(base, body),
  update: (id: string, body: UpdateMetadataRequest) => api.put<MetadataDto>(`${base}/${id}`, body),
  remove: (id: string) => api.delete<void>(`${base}/${id}`),
};

export const METADATA_TYPE_OPTIONS: { value: MetadataType; label: string }[] = [
  { value: MetadataType.Text, label: 'Text' },
  { value: MetadataType.Number, label: 'Number' },
  { value: MetadataType.Date, label: 'Date' },
  { value: MetadataType.Currency, label: 'Currency' },
  { value: MetadataType.Textarea, label: 'Textarea' },
];

export const METADATA_GROUPS: { value: string; label: string }[] = [
  { value: 'B', label: 'Thông tin hợp đồng' },
  { value: 'C', label: 'Bên A — Khách hàng' },
  { value: 'D', label: 'Số tiền bảo hiểm' },
  { value: 'F', label: 'Điều khoản' },
  { value: 'G', label: 'Mức khấu trừ' },
  { value: 'I', label: 'Phí bảo hiểm' },
  { value: 'J', label: 'Thanh toán' },
  { value: 'K', label: 'Người ký' },
  { value: 'L', label: 'Nội dung' },
  { value: 'M', label: 'Khác' },
];
