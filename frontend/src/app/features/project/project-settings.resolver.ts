import { inject } from '@angular/core';
import { ResolveFn } from '@angular/router';
import { ProjectApiService, ProjectDetail } from '@core/api/project.service';

export const projectSettingsDetailResolver: ResolveFn<ProjectDetail> = (route) => {
  const key = route.paramMap.get('projectKey');
  if (!key) {
    throw new Error('projectKey required');
  }
  return inject(ProjectApiService).getDetailForMemberByKey(key);
};
