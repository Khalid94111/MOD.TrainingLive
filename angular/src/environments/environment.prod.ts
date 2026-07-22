import { Environment } from '@abp/ng.core';

const baseUrl = 'http://localhost:4200';

const oAuthConfig = {
  issuer: 'https://localhost:44324/',
  redirectUri: baseUrl,
  clientId: 'Training_App',
  responseType: 'code',
  scope: 'offline_access Training',
  requireHttps: true,
  impersonation: {
    tenantImpersonation: true,
    userImpersonation: true,
  }
};

export const environment = {
  production: true,
  application: {
    baseUrl,
    name: 'Training',
  },
  oAuthConfig,
  apis: {
    default: {
      url: 'https://localhost:44324',
      rootNamespace: 'MOD.Training',
    },
    Travel: {
      url: 'https://localhost:44324',
      rootNamespace: 'MOD.Training',
    },
    AbpAccountPublic: {
      url: oAuthConfig.issuer,
      rootNamespace: 'AbpAccountPublic',
    },
  },
  remoteEnv: {
    url: '/getEnvConfig',
    mergeStrategy: 'deepmerge'
  }
} as Environment;
