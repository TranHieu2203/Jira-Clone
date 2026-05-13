import { registerLicense } from '@syncfusion/ej2-base';

/**
 * Đăng ký Syncfusion Community License.
 * Key lấy từ: https://www.syncfusion.com/account/manage-trials-and-downloads
 * Mỗi major version (vd 27 → 28) phải regenerate key tương ứng.
 * Validate offline, KHÔNG cần internet runtime.
 *
 * Đặt key vào environment variable `SYNCFUSION_LICENSE_KEY` lúc build,
 * hoặc paste trực tiếp vào constant SYNCFUSION_LICENSE_KEY phía dưới.
 * KHÔNG commit key vào git public repo (Community License key là cá nhân).
 */
const SYNCFUSION_LICENSE_KEY = '';

export function registerSyncfusionLicense(): void {
  if (!SYNCFUSION_LICENSE_KEY) {
    return;
  }
  registerLicense(SYNCFUSION_LICENSE_KEY);
}
