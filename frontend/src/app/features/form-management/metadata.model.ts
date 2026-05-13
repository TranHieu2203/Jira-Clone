/**
 * Khớp với backend enum FormManagement.Domain.MetadataType (số int).
 * KHÔNG đổi giá trị — phải sync với BE.
 */
export enum MetadataType {
  Text = 1,
  Number = 2,
  Date = 3,
  Currency = 4,
  Textarea = 5
}

export const METADATA_TYPE_OPTIONS: Array<{ value: MetadataType; i18nKey: string }> = [
  { value: MetadataType.Text, i18nKey: 'form_mgmt.metadata.type_opt.text' },
  { value: MetadataType.Number, i18nKey: 'form_mgmt.metadata.type_opt.number' },
  { value: MetadataType.Date, i18nKey: 'form_mgmt.metadata.type_opt.date' },
  { value: MetadataType.Currency, i18nKey: 'form_mgmt.metadata.type_opt.currency' },
  { value: MetadataType.Textarea, i18nKey: 'form_mgmt.metadata.type_opt.textarea' }
];

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

/**
 * Quy ước prefix cho biểu mẫu bảo hiểm VN (form.md §3).
 * Dùng để filter sidebar và hiển thị group header.
 */
export const METADATA_GROUPS: Array<{ value: string; i18nKey: string }> = [
  { value: 'B', i18nKey: 'form_mgmt.metadata.group.B' },
  { value: 'C', i18nKey: 'form_mgmt.metadata.group.C' },
  { value: 'D', i18nKey: 'form_mgmt.metadata.group.D' },
  { value: 'F', i18nKey: 'form_mgmt.metadata.group.F' },
  { value: 'G', i18nKey: 'form_mgmt.metadata.group.G' },
  { value: 'I', i18nKey: 'form_mgmt.metadata.group.I' },
  { value: 'J', i18nKey: 'form_mgmt.metadata.group.J' },
  { value: 'K', i18nKey: 'form_mgmt.metadata.group.K' },
  { value: 'L', i18nKey: 'form_mgmt.metadata.group.L' },
  { value: 'M', i18nKey: 'form_mgmt.metadata.group.M' }
];
